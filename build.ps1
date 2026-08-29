param(
    [Parameter(Mandatory)]
    [ValidatePattern('^R(20|21|22|23|24|25|26)$')]
    [string]$RevitVersion,
    [switch]$SkipServer
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$year = @{R20=2020;R21=2021;R22=2022;R23=2023;R24=2024;R25=2025;R26=2026}[$RevitVersion]

Write-Host "=== Building Command Set ($RevitVersion) ==="
dotnet build -c "Release $RevitVersion" "$root\commandset\RevitMCPCommandSet.csproj"
if ($LASTEXITCODE -ne 0) { throw "Command set build failed" }

if (-not $SkipServer) {
    Write-Host "=== Building MCP Server ==="
    Push-Location "$root\server"
    try {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
    } finally {
        Pop-Location
    }

    Write-Host "=== Bundling Runtime ==="
    $configName = "Release $RevitVersion"
    $runtimeDir = "$root\plugin\bin\AddIn $year $configName\revit_mcp_plugin\runtime"
    New-Item -ItemType Directory -Force -Path "$runtimeDir" | Out-Null
    $nodeExe = (Get-Command node.exe -ErrorAction Stop).Source
    Copy-Item "$nodeExe" "$runtimeDir\node.exe" -Force

    # Copy built server files + full node_modules
    robocopy "$root\server\build" "$runtimeDir\build" /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    robocopy "$root\server\node_modules" "$runtimeDir\node_modules" /E /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    Copy-Item "$root\server\package.json" "$runtimeDir\" -Force
    Write-Host "Runtime bundled: $runtimeDir ($((Get-ChildItem $runtimeDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB -as [int]) MB)"
}

Write-Host "=== Build Complete ==="
Write-Host "Command set: commandset\bin\Release $RevitVersion\"
Write-Host "Plugin bundle: plugin\bin\AddIn $year Release $RevitVersion\"
if (-not $SkipServer) {
    Write-Host "MCP server: server\build\index.js"
    Write-Host "Runtime: plugin\bin\AddIn $year Release $RevitVersion\revit_mcp_plugin\runtime\node.exe"
}
