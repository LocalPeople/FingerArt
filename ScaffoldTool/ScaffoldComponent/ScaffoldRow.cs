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
        protected class ScaffoldRow
        {
            private Line innerCurve;// 内大横杆
            private Line outerCurve;// 外大横杆
            private Line[] middleCurves;// 中段大横杆
            private Line[] upOuterCurves;// 护栏

            public ScaffoldRow(ScaffoldCorner startCorner, ScaffoldCorner endCorner)
            {
                XYZ vector1 = (endCorner.InnerPoint - startCorner.InnerPoint).Normalize();
                XYZ vector2 = new XYZ(-vector1.Y, vector1.X, 0);
                XYZ innerPointS, innerPointE, outerPointS, outerPointE;

                /* 内外大横杆基于XY平面避让立杆 */
                innerPointS = startCorner.InnerPoint - vector2 * Global.D/* 内外大横杆基于XY平面避让立杆 */ - vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠起点延长
                innerPointE = endCorner.InnerPoint - vector2 * Global.D/* 内外大横杆基于XY平面避让立杆 */ + vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠终点延长
                outerPointS = startCorner.OuterPoint + vector2 * Global.D/* 内外大横杆基于XY平面避让立杆 */ - vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠起点延长
                outerPointE = endCorner.OuterPoint + vector2 * Global.D/* 内外大横杆基于XY平面避让立杆 */ + vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠终点延长

                /* 内外大横杆基于两端转角是否为直角延长横杆长度 */
                if (startCorner.isRightAngle)
                {
                    if (startCorner.isConcave)
                        outerPointS = startCorner.Other2Points[0] + vector2 * Global.D - vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠起点延长
                    else
                        innerPointS = startCorner.Other2Points[0] - vector2 * Global.D - vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠起点延长
                }
                if (endCorner.isRightAngle)
                {
                    if (endCorner.isConcave)
                        outerPointE = endCorner.Other2Points[1] + vector2 * Global.D + vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠终点延长
                    else
                        innerPointE = endCorner.Other2Points[1] - vector2 * Global.D + vector1 * Global.ROW_BEYOND_DISTANCE;// 横杠终点延长
                }

                this.innerCurve = Line.CreateBound(innerPointS, innerPointE);
                this.outerCurve = Line.CreateBound(outerPointS, outerPointE);

                /* 中段大横杆初始化 */
                // 能否优化结构？
                Line[] listLineStart = startCorner.GetScaffoldRowCut(0);
                Line[] listLineEnd = endCorner.GetScaffoldRowCut(1);
                using (listLineStart[0])
                using (listLineEnd[0])
                using (Line lineOffset = Line.CreateBound(innerPointS - innerCurve.Direction * 100, innerPointE + innerCurve.Direction * 100))
                {
                    double offsetDistance;
                    int offsetCount;
                    if (Global.LGHJ <= 1200 / 304.8)
                    {
                        offsetDistance = (Global.LGHJ - 2 * Global.D) / 3;
                        offsetCount = 2;
                        middleCurves = new Line[2];
                    }
                    else if (Global.LGHJ > 1200 / 304.8 && Global.LGHJ <= 1550 / 304.8)
                    {
                        offsetDistance = (Global.LGHJ - 2 * Global.D) / 4;
                        offsetCount = 3;
                        middleCurves = new Line[3];
                    }
                    else
                    {
                        throw new Exception("横向间距或排距参数不支持大于1550mm");
                    }
                    for (; offsetCount > 0; offsetCount--)
                    {
                        using (Line newLine = lineOffset.CreateOffset(offsetCount * offsetDistance, XYZ.BasisZ) as Line)
                        {
                            IntersectionResultArray resultArray1;
                            IntersectionResultArray resultArray2;
                            newLine.Intersect(listLineStart[0], out resultArray1);
                            if (listLineStart[1] != null && resultArray1 == null)
                                newLine.Intersect(listLineStart[1], out resultArray1);
                            newLine.Intersect(listLineEnd[0], out resultArray2);
                            // 待完善接口，满足所有杆件两端延伸要求
                            middleCurves[offsetCount - 1] = ScaffoldUtil.GetExtendLine(resultArray1.get_Item(0).XYZPoint, resultArray2.get_Item(0).XYZPoint, Global.ROW_BEYOND_DISTANCE, Global.ROW_BEYOND_DISTANCE);
                        }
                    }
                    if (listLineStart[1] != null)
                        listLineStart[1].Dispose();
                }

                /* 护栏初始化 */
                upOuterCurves = new Line[Global.GUARDRAIL_DIVIDED_NUM - 1];
                double upOffset = Global.BJ / Global.GUARDRAIL_DIVIDED_NUM;
                for (int i = 1; i < Global.GUARDRAIL_DIVIDED_NUM; i++)
                {
                    upOuterCurves[i - 1] = ScaffoldUtil.GetExtendLine(startCorner.OuterPoint + new XYZ(0, 0, i * upOffset) + vector2 * Global.D, endCorner.OuterPoint + new XYZ(0, 0, i * upOffset) + vector2 * Global.D, Global.ROW_BEYOND_DISTANCE, Global.ROW_BEYOND_DISTANCE);
                }
            }
            /// <summary>
            /// 返回的横杆已基于XY平面避让立杆
            /// </summary>
            /// <returns></returns>
            public Line[] GetCurves()
            {
                return new Line[2] { innerCurve, outerCurve }.Concat(middleCurves).Concat(upOuterCurves).ToArray();
            }
        }
    }
}
