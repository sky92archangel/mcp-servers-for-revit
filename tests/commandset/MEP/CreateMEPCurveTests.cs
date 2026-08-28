using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateMEPCurveTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateMEPCurve_DuctType_DuctTypeFound()
    {
        var ductTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(DuctType))
            .Cast<DuctType>()
            .ToList();
        await Assert.That(ductTypes.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CreateMEPCurve_PipeType_PipeTypeFound()
    {
        var pipeTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(PipeType))
            .Cast<PipeType>()
            .ToList();
        await Assert.That(pipeTypes.Count).IsGreaterThan(0);
    }
}
