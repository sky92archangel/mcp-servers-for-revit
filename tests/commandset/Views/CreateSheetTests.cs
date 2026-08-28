using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateSheetTests : RevitApiTest
{
    private static Document _doc;
    private static ElementId _titleBlockId;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        var titleBlock = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fs => fs.Category?.BuiltInCategory == BuiltInCategory.OST_TitleBlocks);
        _titleBlockId = titleBlock?.Id ?? ElementId.InvalidElementId;
#if !REVIT2026_OR_GREATER
        _titleBlockId = titleBlock?.Id ?? ElementId.InvalidElementId;
#endif
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateSheet_WithTitleBlock_SheetCreated()
    {
        using var tx = new Transaction(_doc, "Create Sheet");
        tx.Start();
        var sheet = ViewSheet.CreateSheet(_doc, _titleBlockId);
        tx.Commit();
        await Assert.That(sheet).IsNotNull();
    }

    [Test]
    public async Task CreateSheet_SetNumber_NumberApplied()
    {
        using var tx = new Transaction(_doc, "Create Sheet With Number");
        tx.Start();
        var sheet = ViewSheet.CreateSheet(_doc, _titleBlockId);
        sheet.SheetNumber = "A-101";
        tx.Commit();
        await Assert.That(sheet.SheetNumber).IsEqualTo("A-101");
    }

    [Test]
    public async Task CreateSheet_SetName_NameApplied()
    {
        using var tx = new Transaction(_doc, "Create Sheet With Name");
        tx.Start();
        var sheet = ViewSheet.CreateSheet(_doc, _titleBlockId);
        sheet.Name = "Floor Plans";
        tx.Commit();
        await Assert.That(sheet.Name).IsEqualTo("Floor Plans");
    }

    [Test]
    public async Task CreateSheet_RollbackOnFailure_SheetNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ViewSheet)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Sheet"))
        {
            tx.Start();
            ViewSheet.CreateSheet(_doc, _titleBlockId);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ViewSheet)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
