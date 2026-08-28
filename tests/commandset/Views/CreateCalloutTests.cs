using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateCalloutTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
        _floorPlan = ViewPlan.Create(_doc, floorPlanType.Id, _level.Id);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateCallout_ViewSection_CalloutCreated()
    {
        using var tx = new Transaction(_doc, "Create Callout");
        tx.Start();
#if REVIT2023_OR_GREATER
        var vft = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Section);
        if (vft != null)
        {
            var box = new BoundingBoxXYZ { Min = new XYZ(-5, -5, 0), Max = new XYZ(5, 5, 10) };
            var callout = ViewSection.CreateCallout(_doc, _floorPlan.Id, vft.Id, box);
            tx.Commit();
            await Assert.That(callout).IsNotNull();
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
    public async Task CreateCallout_RollbackOnFailure_CalloutNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ViewSection)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Callout"))
        {
            tx.Start();
#if REVIT2023_OR_GREATER
            var vft = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Section);
            if (vft != null)
            {
                var box = new BoundingBoxXYZ { Min = new XYZ(-10, -10, 0), Max = new XYZ(10, 10, 10) };
                ViewSection.CreateCallout(_doc, _floorPlan.Id, vft.Id, box);
            }
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ViewSection)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
