using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class CreateSurfaceElementTests : RevitApiTest
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
    public async Task CreateSurfaceElement_FloorByLoop_FloorCreated()
    {
        using var tx = new Transaction(_doc, "Create Floor");
        tx.Start();
        var floorType = new FilteredElementCollector(_doc).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault();
        if (floorType != null)
        {
            var curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(10, 10, 0), new XYZ(0, 10, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 10, 0), new XYZ(0, 0, 0)));
            var floor = _doc.Create.NewFloor(curveArray, floorType, _level, false);
            tx.Commit();
            await Assert.That(floor).IsNotNull();
        }
        else
        {
            tx.RollBack();
        }
    }
}
