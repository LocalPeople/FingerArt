using Autodesk.Revit.DB;
using ScaffoldTool.ScaffoldComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScaffoldTool
{
    partial class XC_KJScaffold
    {
        interface ISlantRod
        {
            List<Line> GetCurves();
        }

        protected abstract class ScaffoldSlantRod : ISlantRod
        {
            // 左1绘图面
            protected System.Collections.Generic.List<Line> drawingFaceCurves1 = new System.Collections.Generic.List<Line>();
            // 左2绘图面
            protected System.Collections.Generic.List<Line> drawingFaceCurves2 = new System.Collections.Generic.List<Line>();
            protected int count2 = 0;// 左2绘图面底边被斜杆分割数
            // 中绘图面
            protected System.Collections.Generic.List<Line> drawingFaceCurves3 = new System.Collections.Generic.List<Line>();
            protected int count3 = 0;// 中绘图面底边被斜杆分割数
            // 右2绘图面
            protected System.Collections.Generic.List<Line> drawingFaceCurves4 = new System.Collections.Generic.List<Line>();
            protected int count4 = 0;// 右2绘图面底边被斜杆分割数
            // 右1绘图面
            protected System.Collections.Generic.List<Line> drawingFaceCurves5 = new System.Collections.Generic.List<Line>();
            protected System.Collections.Generic.List<Line> slantRodLines;
            protected double[] offsetArray;
            protected int stepSum;
            private double bottomOffset;

            public ScaffoldSlantRod(double bottomOffset)
            {
                this.bottomOffset = bottomOffset;
            }

            protected void ScanDrawingFaceCurves(int stepMin1, int stepMax1, int stepMin2, int stepMax2)
            {
                slantRodLines = new System.Collections.Generic.List<Line>();
                // 之字剪刀撑
                if (stepSum >= stepMin1 && stepSum <= stepMax1)
                {
                    StairRodHandler(drawingFaceCurves1, offsetArray[0]);
                }
                // 一道剪刀撑
                else if (stepSum >= stepMin2 && stepSum <= stepMax2)
                {
                    ScaffoldUtil.ScanVertical(slantRodLines, drawingFaceCurves1, offsetArray[0], Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                }
                // 通用剪刀撑
                else
                {
                    ScaffoldUtil.ScanVertical(slantRodLines, drawingFaceCurves1, offsetArray[0], Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                    if (drawingFaceCurves2.Count > 0)
                        ScaffoldUtil.ScanVerticalAndHorizontal(slantRodLines, drawingFaceCurves2, offsetArray.Skip(1).First(), count2, Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                    if (drawingFaceCurves3.Count > 0)
                        ScaffoldUtil.ScanVerticalAndHorizontal(slantRodLines, drawingFaceCurves3, offsetArray.Skip(1 + count2).First(), count3, Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                    if (drawingFaceCurves4.Count > 0)
                        ScaffoldUtil.ScanVerticalAndHorizontal(slantRodLines, drawingFaceCurves4, offsetArray.Skip(1 + count2 + count3).First(), count4, Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                    ScaffoldUtil.ScanVertical(slantRodLines, drawingFaceCurves5, offsetArray.Last(), Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                }
            }

            private void StairRodHandler(System.Collections.Generic.List<Line> drawingFaceCurves, double spanDistance)
            {
                XYZ pointS = drawingFaceCurves[0].GetEndPoint(0);
                XYZ pointE = drawingFaceCurves[0].GetEndPoint(1);
                XYZ vector2 = drawingFaceCurves[0].Direction.CrossProduct(drawingFaceCurves[1].Direction);
                double currentHeight = Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ;
                double targetHeight = drawingFaceCurves[1].Length;
                bool isMoveEnd = true;
                while (currentHeight <= targetHeight)
                {
                    if (isMoveEnd)
                    {
                        pointE = new XYZ(pointE.X, pointE.Y, pointS.Z + Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                        slantRodLines.Add(Line.CreateBound(pointS + vector2 * Global.D, pointE + vector2 * Global.D));
                    }
                    else
                    {
                        pointS = new XYZ(pointS.X, pointS.Y, pointE.Z + Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ);
                        slantRodLines.Add(Line.CreateBound(pointE + vector2 * Global.D, pointS + vector2 * Global.D));
                    }
                    isMoveEnd = !isMoveEnd;
                    currentHeight += Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ;
                }
                if (Math.Abs(currentHeight - targetHeight - Global.BJ) > 0.1)
                {
                    using (Line line = isMoveEnd ? Line.CreateBound(pointS, new XYZ(pointE.X, pointE.Y, pointS.Z + Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ))
                        : Line.CreateBound(pointE, new XYZ(pointS.X, pointS.Y, pointE.Z + Global.SLANT_ROD_VERTICAL_SPAN * Global.BJ)))
                    {
                        IntersectionResultArray resultArray1;
                        line.Intersect(drawingFaceCurves[2], out resultArray1);
                        if (resultArray1 != null)
                            slantRodLines.Add(Line.CreateBound(line.GetEndPoint(0) + vector2 * Global.D, resultArray1.get_Item(0).XYZPoint + vector2 * Global.D));
                    }
                }
            }

            protected abstract void SetOffsetArray(int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList);

            /// <summary>
            /// 用户指定跨数时调用
            /// </summary>
            /// <param name="finalRodHorizontalSpan"></param>
            /// <param name="points"></param>
            protected void SetOffsetArrayByHorizontalSpan(int finalRodHorizontalSpan, System.Collections.Generic.List<XYZ> points)
            {
                offsetArray = new double[(points.Count - 1) / finalRodHorizontalSpan];
                int arrayLeft = 0;
                int arrayRight = offsetArray.Length - 1;
                int pointsLeft = 0;
                int pointsRight = points.Count - 1;
                while (arrayRight >= arrayLeft)
                {
                    offsetArray[arrayLeft++] = points[pointsLeft].DistanceTo(points[pointsLeft + finalRodHorizontalSpan]);
                    pointsLeft += finalRodHorizontalSpan;
                    if (arrayRight >= arrayLeft)
                    {
                        offsetArray[arrayRight--] = points[pointsRight - finalRodHorizontalSpan].DistanceTo(points[pointsRight]);
                        pointsRight -= finalRodHorizontalSpan;
                    }
                }
                int remainder = stepSum % finalRodHorizontalSpan;
                int leftAdd = remainder % 2 == 0 ? remainder / 2 : remainder / 2 + 1;
                int rightAdd = remainder - leftAdd;
                if (offsetArray.Length > 2)
                {
                    if (leftAdd > 1)
                    {
                        count2 = leftAdd - 1;
                        if (rightAdd > 1)
                        {
                            count4 = rightAdd - 1;
                        }
                    }
                    count3 = offsetArray.Length - 2 - count2 - count4;
                    arrayLeft = 0;
                    arrayRight = offsetArray.Length - 1;
                    while (leftAdd > 0 || rightAdd > 0)
                    {
                        offsetArray[arrayLeft++] += Global.LGZJ;
                        leftAdd--;
                        if (rightAdd > 0)
                        {
                            offsetArray[arrayRight--] += Global.LGZJ;
                            rightAdd--;
                        }
                    }
                }
                else
                {
                    offsetArray[0] += leftAdd * Global.LGZJ;
                    offsetArray[1] += rightAdd * Global.LGZJ;
                }
            }

            protected void SetDrawingFaceCurves(XC_GenericModel gm, int index, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList, int stepMin, double topOffset)
            {
                double start = gm.LevelSet[0].Elevation;
                double offset1 = gm.LevelSet.Base2BaseOffset;
                double offset2 = gm.LevelSet.Top2BaseOffset + topOffset;
                XYZ startPoint = scaffoldCornerList[index].OuterPoint;
                XYZ endPoint = scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].OuterPoint;
                XYZ pointA = new XYZ(startPoint.X, startPoint.Y, start + offset1 + Global.PIPE_BASE_OFFSET + bottomOffset);
                XYZ pointB = new XYZ(endPoint.X, endPoint.Y, start + offset1 + Global.PIPE_BASE_OFFSET + bottomOffset);
                XYZ pointC = new XYZ(endPoint.X, endPoint.Y, start + offset2);
                XYZ pointD = new XYZ(startPoint.X, startPoint.Y, start + offset2);
                if (stepSum <= stepMin)
                {
                    ScaffoldUtil.FaceAddCurves(drawingFaceCurves1, pointA, pointB, pointC, pointD);
                }
                else
                {
                    XYZ vector1 = (endPoint - startPoint).Normalize();
                    XYZ point1 = pointA + vector1 * offsetArray[0];
                    XYZ point2 = pointD + vector1 * offsetArray[0];
                    XYZ point3 = pointB - vector1 * offsetArray.Last();
                    XYZ point4 = pointC - vector1 * offsetArray.Last();
                    ScaffoldUtil.FaceAddCurves(drawingFaceCurves1, pointA, point1, point2, pointD);
                    ScaffoldUtil.FaceAddCurves(drawingFaceCurves5, point3, pointB, pointC, point4);
                    if (offsetArray.Length > 2)
                    {
                        if (count2 > 0)
                        {
                            point3 = point1 + vector1 * offsetArray.Skip(1).Take(count2).Sum();
                            point4 = point2 + vector1 * offsetArray.Skip(1).Take(count2).Sum();
                            ScaffoldUtil.FaceAddCurves(drawingFaceCurves2, point1, point3, point4, point2);
                        }
                        if (count3 > 0)
                        {
                            point1 = point1 + vector1 * offsetArray.Skip(1).Take(count2).Sum();
                            point2 = point2 + vector1 * offsetArray.Skip(1).Take(count2).Sum();
                            point3 = point1 + vector1 * offsetArray.Skip(1 + count2).Take(count3).Sum();
                            point4 = point2 + vector1 * offsetArray.Skip(1 + count2).Take(count3).Sum();
                            ScaffoldUtil.FaceAddCurves(drawingFaceCurves3, point1, point3, point4, point2);
                        }
                        if (count4 > 0)
                        {
                            point1 = point1 + vector1 * offsetArray.Skip(1 + count2).Take(count3).Sum();
                            point2 = point2 + vector1 * offsetArray.Skip(1 + count2).Take(count3).Sum();
                            point3 = point1 + vector1 * offsetArray.Skip(1 + count2 + count3).Take(count4).Sum();
                            point4 = point2 + vector1 * offsetArray.Skip(1 + count2 + count3).Take(count4).Sum();
                            ScaffoldUtil.FaceAddCurves(drawingFaceCurves4, point1, point3, point4, point2);
                        }
                    }
                }
            }

            public List<Line> GetCurves()
            {
                return slantRodLines;
            }
        }

        protected class Both4And5SpanSlantRod : ScaffoldSlantRod
        {
            public Both4And5SpanSlantRod(XC_GenericModel gm, int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList, double bottomOffset, double topOffset) : base(bottomOffset)
            {
                SetOffsetArray(index, scaffoldColumnList, scaffoldCornerList);
                SetDrawingFaceCurves(gm, index, scaffoldCornerList, 6, topOffset);
                ScanDrawingFaceCurves(1, 3, 4, 6);
            }

            protected override void SetOffsetArray(int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList)
            {
                System.Collections.Generic.List<XYZ> points = scaffoldCornerList[index].GetEndPoints(1)
                    .Concat((IEnumerable<XYZ>)scaffoldColumnList[index].GetPoints(1))
                    .Concat(scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].GetEndPoints(0))
                    .ToList();
                stepSum = points.Count - 1;
                // 1，2，3之字剪刀撑
                // 4，5，6一道剪刀撑
                if (points.Count <= 7)
                {
                    offsetArray = new double[1];
                    offsetArray[0] = points.First().DistanceTo(points.Last());
                    return;
                }
                // 7特殊剪刀撑
                else if (points.Count == 8)
                {
                    offsetArray = new double[2];
                    offsetArray[0] = points[0].DistanceTo(points[4]);
                    offsetArray[1] = points[4].DistanceTo(points[7]);
                }
                // 19特殊剪刀撑
                else if (points.Count == 20)
                {
                    offsetArray = new double[4];
                    offsetArray[0] = points[0].DistanceTo(points[5]);
                    offsetArray[1] = points[5].DistanceTo(points[10]);
                    count2 = 1;
                    offsetArray[2] = points[10].DistanceTo(points[14]);
                    count3 = 1;
                    offsetArray[3] = points[14].DistanceTo(points[19]);
                }
                // 其他跨数剪刀撑
                else
                {
                    int finalRodHorizontalSpan = points.Count <= 15 ? 4 : Global.SLANT_ROD_HORIZONTAL_SPAN;
                    SetOffsetArrayByHorizontalSpan(finalRodHorizontalSpan, points);
                }
            }
        }

        protected class Only3SpanSlantRod : ScaffoldSlantRod
        {
            public Only3SpanSlantRod(XC_GenericModel gm, int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList, double bottomOffset, double topOffset) : base(bottomOffset)
            {
                SetOffsetArray(index, scaffoldColumnList, scaffoldCornerList);
                SetDrawingFaceCurves(gm, index, scaffoldCornerList, 5, topOffset);
                ScanDrawingFaceCurves(1, 2, 3, 5);
            }

            protected override void SetOffsetArray(int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList)
            {
                System.Collections.Generic.List<XYZ> points = scaffoldCornerList[index].GetEndPoints(1)
                    .Concat((IEnumerable<XYZ>)scaffoldColumnList[index].GetPoints(1))
                    .Concat(scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].GetEndPoints(0))
                    .ToList();
                stepSum = points.Count - 1;
                // 1，2之字剪刀撑
                // 3，4，5一道剪刀撑
                if (points.Count <= 6)
                {
                    offsetArray = new double[1];
                    offsetArray[0] = points.First().DistanceTo(points.Last());
                    return;
                }
                // 其他跨数剪刀撑
                else
                {
                    SetOffsetArrayByHorizontalSpan(3, points);
                }
            }
        }

        protected class EquidistanceSlantRod : ISlantRod
        {
            private int spanCount;
            private double equidistance;
            private System.Collections.Generic.List<Line> drawingFaceCurves1 = new System.Collections.Generic.List<Line>();
            private System.Collections.Generic.List<Line> slantRodLines;
            private double bottomOffset;

            public EquidistanceSlantRod(XC_GenericModel gm, int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList, double bottomOffset, double topOffset)
            {
                this.bottomOffset = bottomOffset;
                SetDrawingFaceCurves(gm, index, scaffoldCornerList, topOffset);
                slantRodLines = new System.Collections.Generic.List<Line>();
                ScaffoldUtil.ScanVerticalAndHorizontal(slantRodLines, drawingFaceCurves1, equidistance, spanCount, Global.SLANT_ROD_EQUIDISTANCE);
            }

            protected void SetDrawingFaceCurves(XC_GenericModel gm, int index, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList, double topOffset)
            {
                double start = gm.LevelSet[0].Elevation;
                double offset1 = gm.LevelSet.Base2BaseOffset;
                double offset2 = gm.LevelSet.Top2BaseOffset + topOffset;
                XYZ startPoint = scaffoldCornerList[index].OuterPoint;
                XYZ endPoint = scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].OuterPoint;
                XYZ pointA = new XYZ(startPoint.X, startPoint.Y, start + offset1 + Global.PIPE_BASE_OFFSET + bottomOffset);
                XYZ pointB = new XYZ(endPoint.X, endPoint.Y, start + offset1 + Global.PIPE_BASE_OFFSET + bottomOffset);
                XYZ pointC = new XYZ(endPoint.X, endPoint.Y, start + offset2);
                XYZ pointD = new XYZ(startPoint.X, startPoint.Y, start + offset2);
                ScaffoldUtil.FaceAddCurves(drawingFaceCurves1, pointA, pointB, pointC, pointD);
                spanCount = (int)Math.Ceiling(startPoint.DistanceTo(endPoint) / Global.SLANT_ROD_EQUIDISTANCE);
                equidistance = startPoint.DistanceTo(endPoint) / spanCount;
            }

            public List<Line> GetCurves()
            {
                return slantRodLines;
            }
        }
    }
}
