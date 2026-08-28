using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class CreatePointElementTests : RevitApiTest
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
    public async Task CreatePointElement_ReferencePoint_PointCreated()
    {
        using var tx = new Transaction(_doc, "Create Reference Point");
        tx.Start();
        var point = _doc.Create.NewReferencePoint(new XYZ(5, 5, 0));
        tx.Commit();
        await Assert.That(point).IsNotNull();
    }

    [Test]
    public async Task CreatePointElement_RollbackOnFailure_PointNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ReferencePoint)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Point"))
        {
            tx.Start();
            _doc.Create.NewReferencePoint(new XYZ(10, 10, 0));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ReferencePoint)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
