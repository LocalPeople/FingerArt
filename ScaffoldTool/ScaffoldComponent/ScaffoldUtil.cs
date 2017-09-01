using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace ScaffoldTool.ScaffoldComponent
{
    class ScaffoldUtil
    {
        public static void DrawCircle(Document doc, XYZ pos, double radius)
        {
            doc.Create.NewModelCurve(Arc.Create(pos + new XYZ(radius / 304.8, 0, 0), pos + new XYZ(radius / 304.8, 0.001, 0), pos + new XYZ(0, -radius / 304.8, 0)), SketchPlane.Create(doc, new Plane(XYZ.BasisZ, new XYZ(0, 0, pos.Z))));
        }

        public static void DrawCurve(Document doc, Curve curve)
        {
            XYZ endPoint0 = curve.GetEndPoint(0);
            XYZ endPoint1 = curve.GetEndPoint(1);
            XYZ vector = (endPoint1 - endPoint0).Normalize();
            if (!vector.IsAlmostEqualTo(XYZ.BasisZ) && !vector.IsAlmostEqualTo(-XYZ.BasisZ))
                using (Plane plane = new Plane(vector.CrossProduct(XYZ.BasisZ), endPoint0))
                using (SketchPlane sketchPlane = SketchPlane.Create(doc, plane))
                    doc.Create.NewModelCurve(curve, sketchPlane);
            else
                using (Plane plane = new Plane(XYZ.BasisY, endPoint0))
                using (SketchPlane sketchPlane = SketchPlane.Create(doc, plane))
                    doc.Create.NewModelCurve(curve, sketchPlane);
        }

        public static bool IsConcavePoint(XYZ point1, XYZ point2, XYZ point3)
        {
            XYZ vector1 = (point2 - point1).Normalize();
            XYZ vector2 = (point3 - point2).Normalize();
            return vector1.CrossProduct(vector2).Normalize().IsAlmostEqualTo(-XYZ.BasisZ);
        }

        public static Line GetExtendLine(XYZ endPoint0, XYZ endPoint1, double extend0, double extend1)
        {
            XYZ vector = (endPoint1 - endPoint0).Normalize();
            return Line.CreateBound(endPoint0 - vector * extend0, endPoint1 + vector * extend1);
        }

        public static void FaceAddCurves(List<Line> drawingFaceCurves, XYZ point1, XYZ point2, XYZ point3, XYZ point4)
        {
            drawingFaceCurves.Add(Line.CreateBound(point1, point2));
            drawingFaceCurves.Add(Line.CreateBound(point2, point3));
            drawingFaceCurves.Add(Line.CreateBound(point3, point4));
            drawingFaceCurves.Add(Line.CreateBound(point4, point1));
        }

        public static void ScanVertical(List<Line> slantRodLines, List<Line> drawingFaceCurves, double spanDistance, double verticalDistance)
        {
            VerticalMethod(slantRodLines, drawingFaceCurves, spanDistance, verticalDistance, false).Dispose();
            VerticalMethod(slantRodLines, drawingFaceCurves, spanDistance, verticalDistance, true).Dispose();
        }

        public static void ScanVerticalAndHorizontal(List<Line> slantRodLines, List<Line> drawingFaceCurves, double spanDistance, int spanCount, double verticalDistance)
        {
            using (Line lineS1 = VerticalMethod(slantRodLines, drawingFaceCurves, spanDistance, verticalDistance, false), lineS2 = VerticalMethod(slantRodLines, drawingFaceCurves, spanDistance, verticalDistance, true))
            {
                HorizontalMethod(slantRodLines, drawingFaceCurves, lineS1, spanDistance, spanCount, false);
                HorizontalMethod(slantRodLines, drawingFaceCurves, lineS2, spanDistance, spanCount, true);
            }
        }

        private static void HorizontalMethod(List<Line> slantRodLines, List<Line> drawingFaceCurves, Line lineS, double spanDistance, int spanCount, bool reverse)
        {
            XYZ vector1 = (drawingFaceCurves[0].GetEndPoint(1) - drawingFaceCurves[0].GetEndPoint(0)).Normalize();
            XYZ vector2 = drawingFaceCurves[0].Direction.CrossProduct(drawingFaceCurves[1].Direction);
            double offset1 = reverse ? 2 * Global.D : Global.D;
            for (int i = 1; i < spanCount; i++)
            {
                Transform offsetTrf = reverse ? Transform.CreateTranslation(-vector1 * i * spanDistance)
                    : Transform.CreateTranslation(vector1 * i * spanDistance);
                using (offsetTrf)
                {
                    XYZ point1, point2;
                    using (Curve offsetLine = lineS.CreateTransformed(offsetTrf))
                    {
                        IntersectionResultArray resultArray2;
                        offsetLine.Intersect(drawingFaceCurves[0], out resultArray2);
                        point1 = resultArray2.get_Item(0).XYZPoint;
                        offsetLine.Intersect(reverse ? drawingFaceCurves[3] : drawingFaceCurves[1], out resultArray2);
                        if (resultArray2 != null)
                            point2 = resultArray2.get_Item(0).XYZPoint;
                        else
                        {
                            offsetLine.Intersect(drawingFaceCurves[2], out resultArray2);
                            point2 = resultArray2.get_Item(0).XYZPoint;
                        }
                    }
                    point1 = point1 + vector2 * offset1;
                    point2 = point2 + vector2 * offset1;
                    slantRodLines.Add(Line.CreateBound(point1, point2));
                }
            }
        }

        private static Line VerticalMethod(List<Line> slantRodLines, List<Line> drawingFaceCurves, double spanDistance, double verticalDistance, bool reverse)
        {
            XYZ pointA = drawingFaceCurves[0].GetEndPoint(0);
            XYZ pointB = drawingFaceCurves[0].GetEndPoint(1);
            XYZ pointC = drawingFaceCurves[1].GetEndPoint(1);
            XYZ vector1 = (pointB - pointA).Normalize();
            XYZ vector2 = (pointC - pointB).Normalize();
            XYZ vector3 = drawingFaceCurves[0].Direction.CrossProduct(drawingFaceCurves[1].Direction);
            XYZ pointS = reverse ? pointB : pointA;
            XYZ pointE = reverse ? pointB - vector1 * spanDistance + vector2 * verticalDistance
                : pointA + vector1 * spanDistance + vector2 * verticalDistance;
            XYZ vector4 = (pointE - pointS).Normalize();
            Line lineS = Line.CreateBound(pointS - vector4 * 1000, pointS + vector4 * 1000);
            double offset1 = reverse ? 2 * Global.D : Global.D;
            // 解决lineS存在误差不通过pointS的Bug
            IntersectionResultArray resultArray1;
            lineS.Intersect(reverse ? drawingFaceCurves[3] : drawingFaceCurves[1], out resultArray1);
            if (resultArray1 == null)
                lineS.Intersect(drawingFaceCurves[2], out resultArray1);
            slantRodLines.Add(Line.CreateBound(pointS + vector3 * offset1, resultArray1.get_Item(0).XYZPoint + vector3 * offset1));
            double currentHeight = verticalDistance;
            double targetHeight = drawingFaceCurves[1].Length - 1;
            while (currentHeight < targetHeight)
            {
                using (Transform offsetTrf = Transform.CreateTranslation(new XYZ(0, 0, currentHeight)))
                {
                    XYZ point1, point2;
                    using (Curve offsetLine = lineS.CreateTransformed(offsetTrf))
                    {
                        IntersectionResultArray resultArray2;
                        offsetLine.Intersect(reverse ? drawingFaceCurves[1] : drawingFaceCurves[3], out resultArray2);
                        point1 = resultArray2.get_Item(0).XYZPoint;
                        offsetLine.Intersect(drawingFaceCurves[2], out resultArray2);
                        if (resultArray2 != null)
                            point2 = resultArray2.get_Item(0).XYZPoint;
                        else
                        {
                            offsetLine.Intersect(reverse ? drawingFaceCurves[3] : drawingFaceCurves[1], out resultArray2);
                            point2 = resultArray2.get_Item(0).XYZPoint;
                        }
                    }
                    point1 = point1 + vector3 * offset1;
                    point2 = point2 + vector3 * offset1;
                    slantRodLines.Add(Line.CreateBound(point1, point2));
                    currentHeight += verticalDistance;
                }
            }
            return lineS;
        }
    }
}
