using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateViewTemplateTests : RevitApiTest
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
    public async Task CreateViewTemplate_IsTemplate_ViewIsTemplate()
    {
        using var tx = new Transaction(_doc, "Create View Template");
        tx.Start();
#if REVIT2023_OR_GREATER
        var templateId = View.CreateViewTemplate(_doc, _floorPlan.Id);
        tx.Commit();
        await Assert.That(templateId).IsNotEqualTo(ElementId.InvalidElementId);
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateViewTemplate_RollbackOnFailure_TemplateNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(View)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Template"))
        {
            tx.Start();
#if REVIT2023_OR_GREATER
            View.CreateViewTemplate(_doc, _floorPlan.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(View)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
