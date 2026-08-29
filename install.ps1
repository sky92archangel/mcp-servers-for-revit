$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# Detect latest build output
$buildDirs = Get-ChildItem "$root\plugin\bin\AddIn *" -Directory | Sort-Object Name -Descending
if (-not $buildDirs) {
    throw "No build output found under plugin\bin\AddIn *. Run build.ps1 first."
}
$buildDir = $buildDirs[0].FullName
Write-Host "Using build output: $buildDir"

# Extract Revit year from directory name (e.g., "AddIn 2026 Release R26" -> "2026")
$year = ($buildDir -split '\\|/')[-1] -replace '^AddIn (\d{4}).*', '$1'
$target = "$env:APPDATA\Autodesk\Revit\Addins\$year"
$pluginTarget = "$target\revit_mcp_plugin"

Write-Host "Target: $target"

# Remove old installation (gracefully handle locked files)
if (Test-Path $pluginTarget) {
    # Use robocopy to delete: mirror from an empty directory
    $emptyDir = "$env:TEMP\_empty_for_rmdir"
    if (-not (Test-Path $emptyDir)) { New-Item -ItemType Directory -Force -Path $emptyDir | Out-Null }
    robocopy $emptyDir $pluginTarget /MIR /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
    Remove-Item -LiteralPath $pluginTarget -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "$target\mcp-servers-for-revit.addin") {
    Remove-Item -LiteralPath "$target\mcp-servers-for-revit.addin" -Force -ErrorAction SilentlyContinue
}

# Copy new files (use robocopy to handle locked files gracefully)
robocopy "$buildDir\revit_mcp_plugin" "$pluginTarget" /E /R:2 /W:2 /NP /NS /NC /NFL /NDL /NJH /NJS 2>$null | Out-Null
Copy-Item -LiteralPath "$buildDir\mcp-servers-for-revit.addin" -Destination "$target\" -Force -ErrorAction SilentlyContinue

# Generate commandRegistry.json from command.json
$commandJsonFile = "$pluginTarget\Commands\RevitMCPCommandSet\command.json"
$registryFile = "$pluginTarget\Commands\commandRegistry.json"
if (Test-Path $commandJsonFile) {
    Write-Host "Generating command registry from command.json..."
    $commandList = Get-Content $commandJsonFile -Raw | ConvertFrom-Json | Select-Object -ExpandProperty commands
    $registry = [PSCustomObject]@{
        Commands = $commandList | ForEach-Object {
            [PSCustomObject]@{
                commandName            = $_.commandName
                assemblyPath           = "RevitMCPCommandSet\{VERSION}\RevitMCPCommandSet.dll"
                enabled                = $true
                supportedRevitVersions = @([int]$year)
                developer              = $_.developer
                description            = $_.description
            }
        }
    }
    $registry | ConvertTo-Json -Depth 5 | Set-Content $registryFile -Encoding UTF8
    Write-Host "commandRegistry.json generated with $($commandList.Count) commands"
}

Write-Host "=== Install Complete ==="
Write-Host "Installed to: $target"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Restart Revit (if running)"
Write-Host "2. Click 'Revit MCP Switch' in Add-Ins ribbon to start the service"
Write-Host "3. Configure AI client with the local runtime:"
Write-Host ""
Write-Host "   Claude Desktop config:"
Write-Host '   {'
Write-Host '       "mcpServers": {'
Write-Host '           "mcp-server-for-revit": {'
Write-Host "               ""command"": ""$pluginTarget\runtime\node.exe"","
Write-Host "               ""args"": [""$pluginTarget\runtime\build\index.js""]"
Write-Host '           }'
Write-Host '       }'
Write-Host '   }'
