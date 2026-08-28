namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 跨版本兼容性扩展方法
    /// 封装 Revit R20-R26 之间的 API 差异
    /// </summary>
    public static class VersionCompat
    {
        // ===== Point.X/Y/Z 兼容 =====
        /// <summary>
        /// 获取 Point 的 X 坐标，兼容 R20-R26
        /// R24+ 使用 p.X，R23- 使用 p.Coordinate.X
        /// </summary>
#if REVIT2024_OR_GREATER
        public static double GetX(this Point p) => p.X;
        public static double GetY(this Point p) => p.Y;
        public static double GetZ(this Point p) => p.Z;
#else
        public static double GetX(this Point p) => p.Coordinate.X;
        public static double GetY(this Point p) => p.Coordinate.Y;
        public static double GetZ(this Point p) => p.Coordinate.Z;
#endif

        // ===== RevisionCloud.Create 兼容 =====
        /// <summary>
        /// 创建修订云线，兼容 R20-R26
        /// R24+ 第4参数为 ElementId，R23- 第4参数为 View 对象
        /// </summary>
        public static RevisionCloud CreateRevisionCloud(
            Document doc, ElementId revisionId, IList<CurveLoop> loops, ElementId viewId)
        {
#if REVIT2024_OR_GREATER
            return RevisionCloud.Create(doc, revisionId, loops, viewId);
#else
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, revisionId, loops, view);
#endif
        }

        // ===== Revision.RevisionNumber 兼容 =====
        /// <summary>
        /// 设置修订编号，兼容 R20-R26
        /// R24+ RevisionNumber 为只读属性，需通过参数设置
        /// </summary>
        public static void SetRevisionNumber(this Revision revision, string number)
        {
#if REVIT2024_OR_GREATER
            revision.get_Parameter(BuiltInParameter.PROJECT_REVISION_REVISION_NUM).Set(number);
#else
            revision.RevisionNumber = number;
#endif
        }

        // ===== ReferencePlane.Create 兼容 =====
        /// <summary>
        /// 创建参照平面，兼容 R20-R26
        /// R24+ 使用 ReferencePlane.Create(doc, plane)
        /// R23- 使用 doc.Create.NewReferencePlane(...)
        /// </summary>
        public static ReferencePlane CreateReferencePlane(Document doc, Plane plane)
        {
#if REVIT2024_OR_GREATER
            return ReferencePlane.Create(doc, plane);
#else
            return doc.Create.NewReferencePlane(
                plane.Origin, plane.Origin + plane.XVec, plane.Normal, doc.ActiveView);
#endif
        }

        // ===== TextNote.Rotation 兼容 =====
        /// <summary>
        /// 设置文字注释旋转角度，兼容 R20-R26
        /// R24+ 有 Rotation 属性，R23- 需通过 RotateElement 实现
        /// </summary>
        public static void SetTextNoteRotation(Document doc, TextNote textNote, double rotation)
        {
#if REVIT2024_OR_GREATER
            textNote.Rotation = rotation;
#else
            XYZ center = textNote.Coord;
            Line axis = Line.CreateUnbound(center, XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, textNote.Id, axis, rotation);
#endif
        }

        // ===== DisplayStyle 兼容 =====
        /// <summary>
        /// 获取显示样式，兼容 R20-R26
        /// </summary>
        public static DisplayStyle GetDisplayStyle(string styleName)
        {
            switch (styleName.ToLower())
            {
                case "wireframe":
                    return DisplayStyle.Wireframe;
                case "hidden":
                case "hiddenline":
                    return DisplayStyle.HiddenLine;
                case "shaded":
                    return DisplayStyle.Shaded;
                case "consistent_colors":
                    return DisplayStyle.ConsistentColors;
                case "realistic":
                    return DisplayStyle.Realistic;
                case "raytrace":
                    return DisplayStyle.Raytrace;
                case "flatcolors":
                    return DisplayStyle.FlatColors;
                case "graphics":
                    return DisplayStyle.Graphics;
                default:
                    return DisplayStyle.HiddenLine;
            }
        }

        // ===== ViewSchedule 属性兼容 =====
        /// <summary>
        /// 设置视图计划表头可见性，兼容 R20-R26
        /// R24+ 有 ShowHeaders 属性，R23- 需通过参数设置
        /// </summary>
        public static void SetScheduleShowHeaders(ViewSchedule schedule, bool show)
        {
#if REVIT2024_OR_GREATER
            schedule.ShowHeaders = show;
#else
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_HEADER).Set(show ? 1 : 0);
#endif
        }

        /// <summary>
        /// 设置视图计划网格线可见性，兼容 R20-R26
        /// </summary>
        public static void SetScheduleShowGridLines(ViewSchedule schedule, bool show)
        {
#if REVIT2024_OR_GREATER
            schedule.ShowGridLines = show;
#else
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_GRID_LINES).Set(show ? 1 : 0);
#endif
        }

        /// <summary>
        /// 设置视图计划轮廓线可见性，兼容 R20-R26
        /// </summary>
        public static void SetScheduleShowOutlines(ViewSchedule schedule, bool show)
        {
#if REVIT2024_OR_GREATER
            schedule.ShowOutlines = show;
#else
            schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_OUTLINES).Set(show ? 1 : 0);
#endif
        }

        // ===== Wall.GetLocation 兼容 =====
        /// <summary>
        /// 获取墙体的定位曲线，兼容 R20-R26
        /// </summary>
        public static Curve GetWallLocationCurve(Wall wall)
        {
            LocationCurve locCurve = wall.Location as LocationCurve;
            return locCurve?.Curve;
        }

        // ===== NurbsSpline / NurbSpline 兼容 =====
        /// <summary>
        /// 创建 NURBS 样条曲线，兼容 R20-R26
        /// R22+ 类名为 NurbSpline，R20-R21 类名为 NurbsSpline
        /// </summary>
        public static Curve CreateNurbSpline(IList<XYZ> points)
        {
#if REVIT2022_OR_GREATER
            var controlPoints = new List<XYZ>(points);
            return NurbSpline.Create(controlPoints, 3); // 3阶样条曲线
#else
            var controlPoints = new List<XYZ>(points);
            return NurbsSpline.Create(controlPoints, 3);
#endif
        }

        // ===== ViewDuplicateOption.Dependent 兼容 =====
        /// <summary>
        /// 获取依赖视图复制选项，兼容 R20-R26
        /// R24+ 有 ViewDuplicateOption.Dependent
        /// </summary>
        public static ViewDuplicateOption GetDependentDuplicateOption()
        {
#if REVIT2024_OR_GREATER
            return ViewDuplicateOption.Dependent;
#else
            return ViewDuplicateOption.Duplicate;
#endif
        }
    }
}
