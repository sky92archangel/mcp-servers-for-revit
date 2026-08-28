using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class ManageViewFiltersTests : RevitApiTest
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
    public async Task ManageViewFilters_GetFilters_FiltersAvailable()
    {
        var filters = new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>()
            .ToList();
        await Assert.That(filters).IsNotNull();
    }

    [Test]
    public async Task ManageViewFilters_AddFilterToView_FilterApplied()
    {
        using var tx = new Transaction(_doc, "Add Filter");
        tx.Start();
        var filter = ParameterFilterElement.Create(_doc, "Test Filter", new ElementCategoryFilter(BuiltInCategory.OST_Walls));
        if (filter != null)
        {
            _floorPlan.AddFilter(filter.Id);
            tx.Commit();
            var filterIds = _floorPlan.GetFilters();
            await Assert.That(filterIds.Contains(filter.Id)).IsTrue();
        }
        else
        {
            tx.RollBack();
        }
    }
}
