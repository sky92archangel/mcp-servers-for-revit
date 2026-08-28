using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateElevationViewTests : RevitApiTest
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
    public async Task CreateElevationView_ElevationMarker_MarkerCreated()
    {
        using var tx = new Transaction(_doc, "Create Elevation Marker");
        tx.Start();
#if REVIT2023_OR_GREATER
        var vft = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Elevation);
        if (vft != null)
        {
            var marker = ElevationMarker.CreateElevationMarker(_doc, vft.Id, _level.Id, new XYZ(0, 0, _level.Elevation));
            tx.Commit();
            await Assert.That(marker).IsNotNull();
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
    public async Task CreateElevationView_RollbackOnFailure_MarkerNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ElevationMarker)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Elevation"))
        {
            tx.Start();
#if REVIT2023_OR_GREATER
            var vft = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Elevation);
            if (vft != null)
                ElevationMarker.CreateElevationMarker(_doc, vft.Id, _level.Id, new XYZ(10, 0, _level.Elevation));
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ElevationMarker)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
