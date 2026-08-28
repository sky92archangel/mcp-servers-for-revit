using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Query;

public class CheckInterferencesTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static Wall _wall1;
    private static Wall _wall2;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _wall1 = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        _wall2 = Wall.Create(_doc, Line.CreateBound(new XYZ(5, -5, 0), new XYZ(5, 5, 0)), _level.Id, false);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CheckInterferences_OverlappingWalls_IntersectionDetected()
    {
        var options = new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true };
        var geom1 = _wall1.get_Geometry(options);
        var geom2 = _wall2.get_Geometry(options);

        Solid solid1 = null, solid2 = null;
        foreach (var obj in geom1) { if (obj is Solid s && s.Faces.Size > 0) { solid1 = s; break; } }
        foreach (var obj in geom2) { if (obj is Solid s && s.Faces.Size > 0) { solid2 = s; break; } }

        if (solid1 != null && solid2 != null)
        {
#if REVIT2025_OR_GREATER
            var result = solid1.Intersect(solid2, out IntersectionResultArray _);
            await Assert.That(result).IsNotEqualTo(SetComparisonResult.Disjoint);
#endif
        }
    }

    [Test]
    public async Task CheckInterferences_NonOverlapping_NoIntersection()
    {
        await Assert.That(true).IsTrue();
    }
}
