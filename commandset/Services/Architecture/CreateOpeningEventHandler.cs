using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateOpeningEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<OpeningCreationInfo> OpeningData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<OpeningCreationInfo> data)
        {
            OpeningData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in OpeningData)
                {
                    if (info.HostElementId <= 0)
                    {
                        _warnings.Add("Host element ID is required for opening creation");
                        continue;
                    }

                    Element hostElement = _doc.GetElement(new ElementId(info.HostElementId));
                    if (hostElement == null)
                    {
                        _warnings.Add($"Host element with ID {info.HostElementId} not found");
                        continue;
                    }

                    using (Transaction tx = new Transaction(_doc, "Create Opening"))
                    {
                        tx.Start();

                        try
                        {
                            Opening opening = null;
                            double widthInFeet = info.Width / 304.8;
                            double heightInFeet = info.Height / 304.8;
                            double sillInFeet = info.SillHeight / 304.8;

                            if (info.OpeningType == OpeningType.WallOpening && hostElement is Wall)
                            {
                                // Create wall opening using rectangle
                                Wall hostWall = hostElement as Wall;
                                XYZ location = info.Location != null ? JZPoint.ToXYZ(info.Location) : null;
                                if (location == null)
                                {
                                    location = VersionCompat.GetWallLocationCurve(hostWall)?.Evaluate(0.5, true);
                                }

                                // Use Opening.Add for rectangular wall openings
                                opening = Opening.Add(hostWall, new XYZ(location.X - widthInFeet / 2, location.Y, location.Z + sillInFeet),
                                    new XYZ(location.X + widthInFeet / 2, location.Y, location.Z + sillInFeet + heightInFeet));
                            }
                            else if (info.OpeningType == OpeningType.FloorOpening && hostElement is Floor)
                            {
                                Floor hostFloor = hostElement as Floor;
                                if (info.Shape == OpeningShape.Rectangular)
                                {
                                    opening = Opening.Add(hostFloor, new XYZ(0, 0, 0), new XYZ(widthInFeet, heightInFeet, 0));
                                }
                            }
                            else if (info.OpeningType == OpeningType.RoofOpening && hostElement is RoofBase)
                            {
                                RoofBase hostRoof = hostElement as RoofBase;
                                opening = Opening.Add(hostRoof, new XYZ(0, 0, 0), new XYZ(widthInFeet, heightInFeet, 0));
                            }
                            else if (info.OpeningType == OpeningType.ShaftOpening)
                            {
                                // For shafts, use NewOpening on ceiling/floor
                                opening = Opening.Add(hostElement as CeilingAndFloor, new XYZ(0, 0, 0), new XYZ(widthInFeet, heightInFeet, 0));
                            }

                            if (opening != null)
                            {
                                elementIds.Add(opening.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create opening: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} opening(s)";
                if (_warnings.Count > 0)
                {
                    message += "\nWarnings:\n  " + string.Join("\n  ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = elementIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating openings: {ex.Message}",
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

        public string GetName()
        {
            return "Create Opening";
        }
    }
}
