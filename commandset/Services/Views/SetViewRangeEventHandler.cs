using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class SetViewRangeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ViewId { get; private set; }
        public double TopOffset { get; private set; }
        public double CutOffset { get; private set; }
        public double BottomOffset { get; private set; }
        public double ViewDepthOffset { get; private set; }
        public int? TopLevelId { get; private set; }

        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int viewId, double topOffset, double cutOffset, double bottomOffset, double viewDepthOffset, int? topLevelId)
        {
            ViewId = viewId;
            TopOffset = topOffset;
            CutOffset = cutOffset;
            BottomOffset = bottomOffset;
            ViewDepthOffset = viewDepthOffset;
            TopLevelId = topLevelId;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Set View Range"))
                {
                    trans.Start();

                    ViewPlan viewPlan = doc.GetElement(new ElementId(ViewId)) as ViewPlan;
                    if (viewPlan == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = $"View with ID {ViewId} is not a plan view" };
                        return;
                    }

                    ViewRange viewRange = viewPlan.GetViewRange();

                    if (TopLevelId.HasValue)
                    {
                        ElementId levelId = new ElementId(TopLevelId.Value);
                        viewRange.SetLevelId(PlanViewPlane.TopClipPlane, levelId);
                    }

                    double offsetFt = TopOffset / 304.8;
                    viewRange.SetOffset(PlanViewPlane.TopClipPlane, offsetFt);

                    offsetFt = CutOffset / 304.8;
                    viewRange.SetOffset(PlanViewPlane.CutPlane, offsetFt);

                    offsetFt = BottomOffset / 304.8;
                    viewRange.SetOffset(PlanViewPlane.BottomClipPlane, offsetFt);

                    offsetFt = ViewDepthOffset / 304.8;
                    viewRange.SetOffset(PlanViewPlane.ViewDepthPlane, offsetFt);

                    viewPlan.SetViewRange(viewRange);

                    trans.Commit();

                    Result = new AIResult<bool>
                    {
                        Success = true,
                        Message = "View range updated successfully",
                        Response = true
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error setting view range: {ex.Message}",
                    Response = false
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Set View Range";
    }
}
