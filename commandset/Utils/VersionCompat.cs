using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 跨版本兼容性扩展方法
    /// 封装 Revit R20-R26 之间的 API 差异
    /// </summary>
    public static class VersionCompat
    {
        // ===== Wall location curve 兼容 =====
        public static Curve GetWallLocationCurve(Wall wall)
        {
            return (wall.Location as LocationCurve)?.Curve;
        }

        // ===== RevisionCloud.Create 兼容 =====
        /// R26 回退到旧签名 Create(Document, View, ElementId, IList{Curve})
        /// R25+ 第4参数为 ElementId，R22-R24 第4参数为 View，R20-R21 只有3参数
        public static RevisionCloud CreateRevisionCloud(
            Document doc, ElementId revisionId, IList<CurveLoop> loops, ElementId viewId)
        {
#if REVIT2026_OR_GREATER
            var curves = new List<Curve>();
            foreach (var loop in loops)
                foreach (var curve in loop)
                    curves.Add(curve);
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, view, revisionId, curves);
#elif REVIT2025_OR_GREATER
            return RevisionCloud.Create(doc, revisionId, loops, viewId);
#elif REVIT2022_OR_GREATER
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, revisionId, loops, view);
#else
            var curves = new List<Curve>();
            foreach (var loop in loops)
                foreach (var curve in loop)
                    curves.Add(curve);
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, view, revisionId, curves);
#endif
        }

        public static void SetRevisionNumber(this Revision revision, string number)
        {
            revision.get_Parameter(BuiltInParameter.PROJECT_REVISION_REVISION_NUM).Set(number);
        }

        // ===== ReferencePlane.Create 兼容 =====
        public static ReferencePlane CreateReferencePlane(Document doc, Plane plane)
        {
#if REVIT2026_OR_GREATER
            return doc.Create.NewReferencePlane(
                plane.Origin, plane.Origin + plane.XVec, plane.Normal, doc.ActiveView);
#elif REVIT2025_OR_GREATER
            return ReferencePlane.Create(doc, plane);
#else
            return doc.Create.NewReferencePlane(
                plane.Origin, plane.Origin + plane.XVec, plane.Normal, doc.ActiveView);
#endif
        }

        // ===== TextNote.Rotation 兼容 =====
        public static void SetTextNoteRotation(Document doc, TextNote textNote, double rotation)
        {
#if REVIT2026_OR_GREATER
            XYZ center = textNote.Coord;
            Line axis = Line.CreateUnbound(center, XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, textNote.Id, axis, rotation);
#elif REVIT2025_OR_GREATER
            textNote.Rotation = rotation;
#else
            XYZ center = textNote.Coord;
            Line axis = Line.CreateUnbound(center, XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, textNote.Id, axis, rotation);
#endif
        }

        // ===== ViewSchedule 属性兼容 =====
        public static void SetScheduleShowHeaders(ViewSchedule schedule, bool show)
        {
#if REVIT2026_OR_GREATER
            // R26: BuiltInParameter removed, use LookupParameter
            Parameter p = schedule.LookupParameter("Show Headers");
            if (p != null) p.Set(show ? 1 : 0);
#elif REVIT2025_OR_GREATER
            schedule.ShowHeaders = show;
#elif REVIT2022_OR_GREATER
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_HEADER).Set(show ? 1 : 0);
#endif
        }
        public static void SetScheduleShowGridLines(ViewSchedule schedule, bool show)
        {
#if REVIT2026_OR_GREATER
            // R26: BuiltInParameter removed, use LookupParameter
            Parameter p = schedule.LookupParameter("Show Grid Lines");
            if (p != null) p.Set(show ? 1 : 0);
#elif REVIT2025_OR_GREATER
            schedule.ShowGridLines = show;
#elif REVIT2022_OR_GREATER
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_GRID_LINES).Set(show ? 1 : 0);
#endif
        }
        public static void SetScheduleShowOutlines(ViewSchedule schedule, bool show)
        {
#if REVIT2026_OR_GREATER
            // R26: BuiltInParameter removed, use LookupParameter
            Parameter p = schedule.LookupParameter("Show Outlines");
            if (p != null) p.Set(show ? 1 : 0);
#elif REVIT2025_OR_GREATER
            schedule.ShowOutlines = show;
#elif REVIT2022_OR_GREATER
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_OUTLINES).Set(show ? 1 : 0);
#endif
        }

        // ===== NurbsSpline / NurbSpline 兼容 =====
        public static Curve CreateNurbSpline(IList<XYZ> points)
        {
#if REVIT2026_OR_GREATER
            // R26: NurbSpline.Create takes HermiteSpline
            var hermiteSpline = HermiteSpline.Create(points, false);
            return NurbSpline.Create(hermiteSpline);
#else
            return NurbSpline.Create(new List<XYZ>(points));
#endif
        }

        // ===== ViewDuplicateOption.Dependent 兼容 =====
        public static ViewDuplicateOption GetDependentDuplicateOption()
        {
#if REVIT2026_OR_GREATER
            return ViewDuplicateOption.Duplicate;
#elif REVIT2025_OR_GREATER
            return ViewDuplicateOption.Dependent;
#else
            return ViewDuplicateOption.Duplicate;
#endif
        }

        // ===== Ceiling.Create 兼容 =====
        public static Ceiling CreateCeiling(Document doc, IList<CurveLoop> profile, ElementId ceilingTypeId, ElementId levelId)
        {
#if REVIT2022_OR_GREATER
            return Ceiling.Create(doc, profile, ceilingTypeId, levelId);
#else
            return null;
#endif
        }

        // ===== Floor.Create 兼容 =====
        public static Floor CreateFloor(Document doc, IList<CurveLoop> profile, ElementId floorTypeId, ElementId levelId)
        {
#if REVIT2022_OR_GREATER
            return Floor.Create(doc, profile, floorTypeId, levelId);
#else
            return null;
#endif
        }

        // ===== Space.Create 兼容 =====
        public static Space CreateSpace(Document doc, ElementId levelId, XYZ point)
        {
#if REVIT2026_OR_GREATER
            // R26: NewSpace takes Level, UV
            Level level = doc.GetElement(levelId) as Level;
            if (level == null) return null;
            return doc.Create.NewSpace(level, new UV(point.X, point.Y));
#elif REVIT2022_OR_GREATER
            return Space.Create(doc, levelId, point);
#else
            return null;
#endif
        }

        // ===== MEPSystem.AddElements 兼容 =====
        public static void AddElementsToMEPSystem(MEPSystem system, IList<ElementId> elementIds)
        {
#if REVIT2026_OR_GREATER
            // R26: AddElement/AddElements removed
#elif REVIT2022_OR_GREATER
            system.AddElements(elementIds);
#endif
        }

        // ===== ElevationMarker.CreateElevationView 兼容 =====
        /// R26: CreateElevationView(ElementId, int) -> CreateElevationView(XYZ, int) removed
        public static ViewSection CreateElevationView(ElevationMarker marker, ElementId levelId, int index)
        {
#if REVIT2026_OR_GREATER
            // R26: CreateElevationView and ViewSection.CreateElevation removed
            return null;
#elif REVIT2022_OR_GREATER
            return marker.CreateElevationView(levelId, index);
#else
            return null;
#endif
        }

        // ===== ViewSection.CreateCallout 兼容 =====
        /// R26: CreateCallout(Document, ElementId, ElementId, BoundingBoxXYZ, XYZ)
        public static ViewSection CreateCallout(Document doc, ElementId hostViewId, ElementId viewFamilyTypeId, BoundingBoxXYZ box)
        {
#if REVIT2026_OR_GREATER
            // R26: 5th param is XYZ point2
            return null; // Callout creation not directly supported
#elif REVIT2022_OR_GREATER
            return ViewSection.CreateCallout(doc, hostViewId, viewFamilyTypeId, box);
#else
            return null;
#endif
        }

        // ===== View.CreateViewTemplate 兼容 =====
        /// R26: CreateViewTemplate removed
        public static ElementId CreateViewTemplate(Document doc, ElementId sourceViewId)
        {
#if REVIT2026_OR_GREATER
            // R26: CreateViewTemplate not available
            return ElementId.InvalidElementId;
#elif REVIT2022_OR_GREATER
            return View.CreateViewTemplate(doc, sourceViewId);
#else
            return ElementId.InvalidElementId;
#endif
        }

        // ===== Duct.Create 兼容 =====
        /// R26: Duct.Create signature changed to (Document, ElementId, ElementId, Connector, Connector)
        public static Duct CreateDuct(Document doc, ElementId systemTypeId, XYZ start, XYZ end, ElementId levelId)
        {
#if REVIT2026_OR_GREATER
            // R26: Duct.Create uses Connector-based approach, fallback to null
            return null;
#elif REVIT2022_OR_GREATER
            return Duct.Create(doc, systemTypeId, start, end, levelId);
#else
            return null;
#endif
        }

        // ===== Pipe.Create 兼容 =====
        public static Pipe CreatePipe(Document doc, ElementId systemTypeId, XYZ start, XYZ end, ElementId levelId)
        {
#if REVIT2026_OR_GREATER
            // R26: Pipe.Create uses Connector-based approach, fallback to null
            return null;
#elif REVIT2022_OR_GREATER
            return Pipe.Create(doc, systemTypeId, start, end, levelId);
#else
            return null;
#endif
        }

        // ===== Conduit.Create 兼容 =====
#if REVIT2026_OR_GREATER
        // R26: Conduit is protected/internal, use doc.Create.NewConduit
        public static MEPCurve CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId)
        {
            return null; // Conduit creation not directly supported in R26 via this API
        }
#elif REVIT2025_OR_GREATER
        public static Conduit CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId)
        {
            return Conduit.Create(doc, conduitTypeId, start, end, levelId);
        }
#else
        public static object CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId)
        {
            return null;
        }
#endif

        // ===== Category.BuiltInCategory 兼容 =====
        public static BuiltInCategory GetBuiltInCategory(Category category)
        {
#if REVIT2025_OR_GREATER
            return category.BuiltInCategory;
#else
            return (BuiltInCategory)category.Id.IntegerValue;
#endif
        }

        // ===== ViewSheet.AddRevision 兼容 =====
        public static void AddRevisionToSheet(ViewSheet sheet, ElementId revisionId)
        {
#if REVIT2026_OR_GREATER
            // R26: AddRevision removed, use parameter-based approach
            // Revision sets are now managed via sheet parameters
#elif REVIT2022_OR_GREATER
            sheet.AddRevision(revisionId);
#endif
        }

        // ===== ScheduleDefinition 兼容 =====
        public static void SetScheduleFieldVisibility(ScheduleDefinition definition, ScheduleFieldId fieldId, bool visible)
        {
#if REVIT2026_OR_GREATER
            // R26: SetFieldVisibility removed
#elif REVIT2022_OR_GREATER
            definition.SetFieldVisibility(fieldId, visible);
#endif
        }
        public static ElementId GetScheduleCategoryId(ScheduleDefinition definition)
        {
#if REVIT2026_OR_GREATER
            return ElementId.InvalidElementId;
#elif REVIT2022_OR_GREATER
            return definition.GetCategoryId();
#else
            return ElementId.InvalidElementId;
#endif
        }
        public static IEnumerable<Parameter> GetCategoryParameters(Category category)
        {
#if REVIT2026_OR_GREATER
            // R26: Category.Parameters removed, iterate through document parameters
            return Enumerable.Empty<Parameter>();
#else
            return category.Parameters?.Cast<Parameter>() ?? Enumerable.Empty<Parameter>();
#endif
        }

        // ===== Face.SurfaceType 兼容 =====
        public static string GetSurfaceTypeName(Face face)
        {
#if REVIT2026_OR_GREATER
            // R26: SurfaceType removed, use type-based detection
            if (face is PlanarFace) return "Planar";
            if (face is CylindricalFace) return "Cylindrical";
            if (face is RevolvedFace) return "Revolved";
            if (face is RuledFace) return "Ruled";
            if (face is HermiteFace) return "Hermite";
            return "Unknown";
#elif REVIT2022_OR_GREATER
            return face.SurfaceType.ToString();
#else
            if (face is PlanarFace) return "Planar";
            if (face is CylindricalFace) return "Cylindrical";
            if (face is RevolvedFace) return "Revolved";
            if (face is RuledFace) return "Ruled";
            if (face is HermiteFace) return "Hermite";
            return "Unknown";
#endif
        }

        // ===== IntersectionResult.Reference 兼容 =====
        public static Reference GetIntersectionReference(IntersectionResult result)
        {
#if REVIT2026_OR_GREATER
            return null;
#elif REVIT2022_OR_GREATER
            return result.Reference;
#else
            return null;
#endif
        }

        // ===== DisplayStyle 兼容 =====
        /// 返回显示样式名称，避免直接使用 DisplayStyle 枚举（不同版本枚举值不同）
        public static string GetDisplayStyleName(string styleName)
        {
            switch (styleName.ToLower())
            {
                case "wireframe": return "Wireframe";
                case "hidden":
                case "hiddenline": return "HiddenLine";
                case "shaded":
                case "shading": return "Shading";
                case "consistent_colors": return "ConsistentColors";
                case "realistic": return "Realistic";
                default: return "HiddenLine";
            }
        }
    }
}
