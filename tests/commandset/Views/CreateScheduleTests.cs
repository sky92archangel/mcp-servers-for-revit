using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateScheduleTests : RevitApiTest
{
    private static Document _doc;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateSchedule_WallSchedule_ScheduleCreated()
    {
        using var tx = new Transaction(_doc, "Create Schedule");
        tx.Start();
        var collector = new FilteredElementCollector(_doc);
        var wallCategory = collector.OfClass(typeof(WallType)).FirstElement()?.Category;
        var schedule = ViewSchedule.CreateSchedule(_doc, wallCategory?.Id ?? ElementId.InvalidElementId);
        tx.Commit();
        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.IsSchedule).IsTrue();
    }

    [Test]
    public async Task CreateSchedule_RollbackOnFailure_ScheduleNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ViewSchedule)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Schedule"))
        {
            tx.Start();
            var wallCategory = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).FirstElement()?.Category;
            ViewSchedule.CreateSchedule(_doc, wallCategory?.Id ?? ElementId.InvalidElementId);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ViewSchedule)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
