using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateMEPSystemEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<MEPSystemCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<MEPSystemCreationInfo> data)
    {
      CreatedInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var systemIds = new List<int>();
        _warnings.Clear();

        foreach (var data in CreatedInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Create MEP System"))
          {
            transaction.Start();

            ElementId systemTypeId = GetSystemTypeId(data.SystemType);
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
            {
              _warnings.Add($"Unsupported system type: {data.SystemType}");
              transaction.Commit();
              continue;
            }

            MEPSystem system = null;

            // Mechanical systems
            if (data.SystemType.Equals("SupplyAir", StringComparison.OrdinalIgnoreCase) ||
                data.SystemType.Equals("ReturnAir", StringComparison.OrdinalIgnoreCase) ||
                data.SystemType.Equals("ExhaustAir", StringComparison.OrdinalIgnoreCase))
            {
              system = MechanicalSystem.Create(doc, systemTypeId);
            }
            // Plumbing systems
            else if (data.SystemType.Equals("Sanitary", StringComparison.OrdinalIgnoreCase) ||
                     data.SystemType.Equals("HydronicSupply", StringComparison.OrdinalIgnoreCase) ||
                     data.SystemType.Equals("HydronicReturn", StringComparison.OrdinalIgnoreCase))
            {
              system = PipingSystem.Create(doc, systemTypeId);
            }
            else
            {
              _warnings.Add($"Unhandled system type: {data.SystemType}");
            }

            if (system != null)
            {
              if (!string.IsNullOrEmpty(data.Name))
              {
                system.Name = data.Name;
              }

              if (data.ElementIds != null && data.ElementIds.Count > 0)
              {
                List<ElementId> elemIds = data.ElementIds.Select(id => new ElementId(id)).ToList();
                system.AddElements(elemIds);
              }

              systemIds.Add(system.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {systemIds.Count} MEP system(s).";
        if (_warnings.Count > 0)
        {
          message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
        }
        Result = new AIResult<List<int>>
        {
          Success = true,
          Message = message,
          Response = systemIds,
        };
      }
      catch (Exception ex)
      {
        Result = new AIResult<List<int>>
        {
          Success = false,
          Message = $"Error creating MEP system: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating MEP system: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    private ElementId GetSystemTypeId(string systemType)
    {
      BuiltInCategory bic;
      switch (systemType.ToLower())
      {
        case "supplyair":
          bic = BuiltInCategory.OST_MEPSystems;
          break;
        case "returnair":
          bic = BuiltInCategory.OST_MEPSystems;
          break;
        case "exhaustair":
          bic = BuiltInCategory.OST_MEPSystems;
          break;
        case "sanitary":
          bic = BuiltInCategory.OST_PipingSystems;
          break;
        case "hydronicsupply":
          bic = BuiltInCategory.OST_PipingSystems;
          break;
        case "hydronicreturn":
          bic = BuiltInCategory.OST_PipingSystems;
          break;
        default:
          return ElementId.InvalidElementId;
      }

      FilteredElementCollector collector = new FilteredElementCollector(doc)
          .OfCategory(bic)
          .OfClass(typeof(MEPSystemType));

      foreach (MEPSystemType st in collector.Cast<MEPSystemType>())
      {
        if (st.Name.Equals(systemType, StringComparison.OrdinalIgnoreCase))
          return st.Id;
      }

      // Fallback: return first available system type
      MEPSystemType first = collector.Cast<MEPSystemType>().FirstOrDefault();
      return first?.Id ?? ElementId.InvalidElementId;
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Create MEP System";
    }
  }
}
