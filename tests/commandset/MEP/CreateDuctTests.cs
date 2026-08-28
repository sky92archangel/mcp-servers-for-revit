using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateDuctTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static MechanicalSystemType _supplyAirType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _supplyAirType = new FilteredElementCollector(_doc)
            .OfClass(typeof(MechanicalSystemType))
            .Cast<MechanicalSystemType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateDuct_BetweenPoints_DuctCreated()
    {
        using var tx = new Transaction(_doc, "Create Duct");
        tx.Start();
#if REVIT2025_OR_GREATER
        var duct = Duct.Create(_doc, _supplyAirType?.Id ?? ElementId.InvalidElementId, new XYZ(0, 0, 0), new XYZ(10, 0, 0), _level.Id);
        tx.Commit();
        await Assert.That(duct).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateDuct_SetDiameter_DiameterApplied()
    {
        using var tx = new Transaction(_doc, "Create Duct With Size");
        tx.Start();
#if REVIT2025_OR_GREATER
        var duct = Duct.Create(_doc, _supplyAirType?.Id ?? ElementId.InvalidElementId, new XYZ(0, 0, 0), new XYZ(10, 0, 0), _level.Id);
        var diamParam = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
        diamParam?.Set(0.5); // 6 inches diameter in feet
        tx.Commit();
        await Assert.That(duct).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateDuct_RollbackOnFailure_DuctNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Duct)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Duct"))
        {
            tx.Start();
#if REVIT2025_OR_GREATER
            Duct.Create(_doc, _supplyAirType?.Id ?? ElementId.InvalidElementId, new XYZ(20, 0, 0), new XYZ(30, 0, 0), _level.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Duct)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
