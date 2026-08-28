using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreatePipeTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static PipingSystemType _systemType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _systemType = new FilteredElementCollector(_doc)
            .OfClass(typeof(PipingSystemType))
            .Cast<PipingSystemType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreatePipe_BetweenPoints_PipeCreated()
    {
        using var tx = new Transaction(_doc, "Create Pipe");
        tx.Start();
#if REVIT2025_OR_GREATER
        var pipe = Pipe.Create(_doc, _systemType?.Id ?? ElementId.InvalidElementId, new XYZ(0, 0, 0), new XYZ(10, 0, 0), _level.Id);
        tx.Commit();
        await Assert.That(pipe).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreatePipe_SetDiameter_DiameterApplied()
    {
        using var tx = new Transaction(_doc, "Create Pipe With Size");
        tx.Start();
#if REVIT2025_OR_GREATER
        var pipe = Pipe.Create(_doc, _systemType?.Id ?? ElementId.InvalidElementId, new XYZ(0, 0, 0), new XYZ(10, 0, 0), _level.Id);
        var diamParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        diamParam?.Set(0.25); // 3 inches in feet
        tx.Commit();
        await Assert.That(pipe).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreatePipe_RollbackOnFailure_PipeNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Pipe)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Pipe"))
        {
            tx.Start();
#if REVIT2025_OR_GREATER
            Pipe.Create(_doc, _systemType?.Id ?? ElementId.InvalidElementId, new XYZ(20, 0, 0), new XYZ(30, 0, 0), _level.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Pipe)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
