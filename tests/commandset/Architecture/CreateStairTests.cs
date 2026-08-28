using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateStairTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level1;
    private static Level _level2;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level1 = Level.Create(_doc, 0.0);
        _level1.Name = "Level 1";
        _level2 = Level.Create(_doc, 10.0);
        _level2.Name = "Level 2";
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateStair_RunBetweenLevels_RunCreated()
    {
        using var tx = new Transaction(_doc, "Create Stair Run");
        tx.Start();
#if REVIT2023_OR_GREATER
        var stairsType = new FilteredElementCollector(_doc)
            .OfClass(typeof(StairsType))
            .Cast<StairsType>()
            .FirstOrDefault();
        if (stairsType != null)
        {
            var run = StairsRun.Create(_doc, stairsType.Id, _level1.Id, _level2.Id, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), StairsRunJustification.Center);
            tx.Commit();
            await Assert.That(run).IsNotNull();
        }
        else
        {
            tx.RollBack();
        }
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateStair_RollbackOnFailure_StairNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(StairsRun)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Stair"))
        {
            tx.Start();
#if REVIT2023_OR_GREATER
            var stairsType = new FilteredElementCollector(_doc)
                .OfClass(typeof(StairsType))
                .Cast<StairsType>()
                .FirstOrDefault();
            if (stairsType != null)
                StairsRun.Create(_doc, stairsType.Id, _level1.Id, _level2.Id, Line.CreateBound(new XYZ(20, 0, 0), new XYZ(30, 0, 0)), StairsRunJustification.Center);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(StairsRun)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
