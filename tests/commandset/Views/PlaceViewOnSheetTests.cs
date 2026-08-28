using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class PlaceViewOnSheetTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;
    private static ViewSheet _sheet;

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
        var titleBlock = new FilteredElementCollector(_doc)
            .OfClass(typeof(TitleBlockType))
            .Cast<TitleBlockType>()
            .FirstOrDefault();
        _sheet = ViewSheet.CreateSheet(_doc, titleBlock?.Id ?? ElementId.InvalidElementId);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task PlaceViewOnSheet_Viewport_ViewportCreated()
    {
        using var tx = new Transaction(_doc, "Place View on Sheet");
        tx.Start();
        var viewport = Viewport.Create(_doc, _sheet.Id, _floorPlan.Id, new XYZ(0.5, 0.5, 0));
        tx.Commit();
        await Assert.That(viewport).IsNotNull();
    }

    [Test]
    public async Task PlaceViewOnSheet_RollbackOnFailure_ViewportNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Viewport)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Viewport"))
        {
            tx.Start();
            Viewport.Create(_doc, _sheet.Id, _floorPlan.Id, new XYZ(2, 2, 0));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Viewport)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
