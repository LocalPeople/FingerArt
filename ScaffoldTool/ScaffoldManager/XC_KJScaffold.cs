using Autodesk.Revit.DB;
using ScaffoldTool.ScaffoldComponent;
using ScaffoldTool.ScaffoldManager;
using System;
using System.Collections.Generic;
using System.Linq;
using XC.Util;

namespace ScaffoldTool
{
    /// <summary>
    /// 扣件式脚手架数据计算类
    /// </summary>
    internal partial class XC_KJScaffold : ScaffoldBase
    {
        private Document doc;
        private XC_GenericModel gm;
        private List<ScaffoldColumn> scaffoldLineList;
        /// <summary>
        /// 对应scaffoldLineList的开头角点对象集合
        /// </summary>
        private List<ScaffoldCorner> scaffoldCornerList;
        private List<ScaffoldRow> scaffoldRowList;
        private List<ScaffoldConnect> scaffoldConnectList;
        private List<ISlantRod> scaffoldSlantRodList;
        private List<ScaffoldFixture> scaffoldFixtureList;
        private List<ScaffoldBoard> scaffoldBoardList;
        private List<ScaffoldOverhangBeam> scaffoldOverhangBeamList;
        private bool[][] isScaffoldColumnHasBeamUnder;
        public List<Curve> scaffoldCurves1;
        public List<Curve> scaffoldCurves2;

        public XC_GenericModel HostGenericModel { get { return gm; } }

        public XC_KJScaffold(Document doc, XC_GenericModel gm)
        {
            this.doc = doc;
            this.gm = gm;
            scaffoldCurves1 = GeomUtil.GetLargerCurves(gm.TopCurveList, Global.NJJQ * 304.8);
            scaffoldCurves2 = GeomUtil.GetLargerCurves(gm.TopCurveList, (Global.NJJQ + Global.LGHJ) * 304.8);
        }

        // 计算非转角处立杆
        public void CalculateLines()
        {
            scaffoldLineList = new List<ScaffoldColumn>();
            for (int i = 0; i < gm.TopCurveList.Count; i++)
            {
                XYZ pointS, pointE;
                double validLength = SureValidLength(i, out pointS, out pointE);
                scaffoldLineList.Add(new ScaffoldColumn(validLength, pointS, pointE));
            }
            scaffoldLineList.ForEach(line => line.SurePoints(Global.LGHJ, Global.LGZJ));
        }

        // 计算转角处立杆
        public void CalculateCorners()
        {
            scaffoldCornerList = new List<ScaffoldCorner>();
            for (int i = 0; i < scaffoldCurves1.Count && i < scaffoldCurves2.Count; i++)
            {
                XYZ point1 = scaffoldCurves1[i > 0 ? i - 1 : scaffoldCurves1.Count - 1].GetEndPoint(0);
                XYZ point2 = scaffoldCurves1[i].GetEndPoint(0);
                XYZ point3 = scaffoldCurves1[i].GetEndPoint(1);
                XYZ point4 = scaffoldCurves2[i].GetEndPoint(0);
                XYZ vector1 = (point2 - point1).Normalize();
                XYZ vector2 = (point3 - point2).Normalize();
                // 确认角点垂直移动向量
                XYZ vector3 = new XYZ(vector1.Y, -vector1.X, 0);
                XYZ vector4 = new XYZ(vector2.Y, -vector2.X, 0);
                XYZ point5, point6;
                if (ScaffoldUtil.IsConcavePoint(point1, point2, point3))
                {
                    if (Math.Abs(vector3.DotProduct(vector4)) < 0.00174532836589830883577820272085) // 90°阴角
                    {
                        point5 = point4 - vector3 * Global.LGHJ;
                        point6 = point4 - vector4 * Global.LGHJ;
                        scaffoldCornerList.Add(new ScaffoldCorner(point2, point4, point5, point6, true, true));
                    }
                    else // 非90°阴角
                        scaffoldCornerList.Add(new ScaffoldCorner(point2, point4, false));
                }
                else
                {
                    point5 = point2 + vector3 * Global.LGHJ;
                    point6 = point2 + vector4 * Global.LGHJ;
                    if (Math.Abs(vector3.DotProduct(vector4)) < 0.00174532836589830883577820272085) // 90°仰角
                        scaffoldCornerList.Add(new ScaffoldCorner(point2, point4, point5, point6, false, true));
                    else // 非90°仰角
                        scaffoldCornerList.Add(new ScaffoldCorner(point2, point4, point5, point6, false, false));
                }
            }
        }

        // 计算横杠
        public void CalculateRows()
        {
            scaffoldRowList = new List<ScaffoldRow>();
            for (int i = 0; i < scaffoldCornerList.Count; i++)
            {
                int iNext = (i + 1) % scaffoldCornerList.Count;
                scaffoldRowList.Add(new ScaffoldRow(scaffoldCornerList[i], scaffoldCornerList[iNext]));
            }
        }

        // 计算连接杆
        public void CalculateConnects()
        {
            scaffoldConnectList = new List<ScaffoldConnect>();
            for (int i = 0; i < scaffoldLineList.Count && i < scaffoldCornerList.Count; i++)
            {
                scaffoldConnectList.Add(new ScaffoldConnect(i, scaffoldLineList, scaffoldCornerList));
            }
        }

        // 计算斜杠
        public void CalculateSlantRods(double bottomOffset, double topOffset)
        {
            scaffoldSlantRodList = new List<ISlantRod>();
            for (int i = 0; i < scaffoldLineList.Count; i++)
            {
                if (Global.SLANT_ROD_EQUIDISTANCE >= -1)
                    scaffoldSlantRodList.Add(new EquidistanceSlantRod(gm, i, scaffoldLineList, scaffoldCornerList, bottomOffset, topOffset));
                else
                {
                    if (Global.SLANT_ROD_HORIZONTAL_SPAN > 3)
                        scaffoldSlantRodList.Add(new Both4And5SpanSlantRod(gm, i, scaffoldLineList, scaffoldCornerList, bottomOffset, topOffset));
                    else
                        scaffoldSlantRodList.Add(new Only3SpanSlantRod(gm, i, scaffoldLineList, scaffoldCornerList, bottomOffset, topOffset));
                }
            }
        }

        // 计算连墙件
        public void CalculateFixtures()
        {
            scaffoldFixtureList = new List<ScaffoldFixture>();
            OnProgressStart(new ProgressStartEventArgs { CurrentProgress = "正在计算连墙件……", ProgressMaximum = scaffoldLineList.Count });
            ScaffoldFixtureBuilder builder = new ScaffoldFixtureBuilder(doc, gm.LevelSet.Height);
            foreach (ScaffoldColumn scaffoldColumn in scaffoldLineList)
            {
                XYZ[] innerPoints = scaffoldColumn.GetPoints(0).Select(p => new XYZ(p.X, p.Y, gm.startElevation)).ToArray();
                if (innerPoints.Length == 0)
                {
                    OnProgressReport(new ProgressReportEventArgs());
                    continue;
                }
                XYZ vector1 = scaffoldColumn.Direction;
                XYZ vector2 = new XYZ(-vector1.Y, vector1.X, 0);
                builder.SetVector(vector1, vector2);
                for (int i = 0; i < innerPoints.Length - 1; i += Global.LQJKS)
                {
                    builder.SetLocation(innerPoints[i]);
                    scaffoldFixtureList.AddRange(builder.Create());
                }
                builder.SetLocation(innerPoints[innerPoints.Length - 1]);
                scaffoldFixtureList.AddRange(builder.Create());
                OnProgressReport(new ProgressReportEventArgs());
            }
        }

        // 计算脚手板
        public void CalculateBoards(double bottomOffset)
        {
            scaffoldBoardList = new List<ScaffoldBoard>();
            ScaffoldBoardBuilder builder = new ScaffoldBoardBuilder(
                gm.startElevation + Global.PIPE_BASE_OFFSET + 1.5 * Global.D + bottomOffset,
                Global.SCAFFOLD_NET_THICK,
                Global.SCAFFOLD_VERTICAL_FLOOR_THICK,
                Global.D);
            for (int i = 0; i < scaffoldCornerList.Count; i++)
            {
                builder.SetBoardPoints(scaffoldCornerList[i].InnerPoint, scaffoldCornerList[i].OuterPoint,
                    scaffoldCornerList[(i + 1) % scaffoldCornerList.Count].InnerPoint, scaffoldCornerList[(i + 1) % scaffoldCornerList.Count].OuterPoint);
                scaffoldBoardList.Add(builder.Create());
            }
        }

        // 计算悬挑梁
        public void CalculateOverhangBeams()
        {
            isScaffoldColumnHasBeamUnder = new bool[scaffoldLineList.Count][];
            for (int i = 0; i < scaffoldLineList.Count; i++)
                isScaffoldColumnHasBeamUnder[i] = new bool[scaffoldLineList[i].GetPoints(0).Count];
            scaffoldOverhangBeamList = new List<ScaffoldOverhangBeam>();

            /* 处理拐角处立杆点悬挑梁 */
            for (int i = 0; i < scaffoldCornerList.Count; i++)
            {
                int iLast = i > 0 ? i - 1 : scaffoldCornerList.Count - 1;

                if (!scaffoldCornerList[i].isRightAngle) continue;

                if (!scaffoldCornerList[i].isConcave)
                {
                    var iInnerPoints = scaffoldLineList[i].GetPoints(0);
                    var iOuterPoints = scaffoldLineList[i].GetPoints(1);
                    var iLastInnerPoints = scaffoldLineList[iLast].GetPoints(0);
                    var iLastOuterPoints = scaffoldLineList[iLast].GetPoints(1);
                    // [0] 上一面除角点外倒数第一个外排立杆点和内排立杆点
                    // [1] 这一面起始角点的外排立杆点和内排立杆点
                    // [2] 这一面除角点外第一个外排立杆点和内排立杆点
                    XYZ[,] array2D_1 = new XYZ[3, 2];
                    array2D_1[1, 0] = scaffoldCornerList[i].OuterPoint;
                    array2D_1[1, 1] = gm.TopCurveList[i].GetEndPoint(0);
                    // 上一面除角点外倒数第二个外排立杆点和这一面除角点外第二个外排立杆点
                    XYZ[] array1D_1 = new XYZ[2];
                    if (iInnerPoints.Count >= 2)
                    {
                        SetScaffoldColumnHasBeamUnder(isScaffoldColumnHasBeamUnder[i], 1);
                        array2D_1[2, 1] = iInnerPoints[0];
                        array2D_1[2, 0] = iOuterPoints[0];
                        array1D_1[1] = iOuterPoints[1];
                    }
                    if (iLastInnerPoints.Count >= 2)
                    {
                        SetScaffoldColumnHasBeamUnder(isScaffoldColumnHasBeamUnder[iLast], 1, false);
                        array2D_1[0, 1] = iLastInnerPoints[iLastInnerPoints.Count - 1];
                        array2D_1[0, 0] = iLastOuterPoints[iLastOuterPoints.Count - 1];
                        array1D_1[0] = iLastOuterPoints[iLastOuterPoints.Count - 2];
                    }
                    scaffoldOverhangBeamList.Add(new ScaffoldOverhangBeam(array2D_1, array1D_1));
                }
                else
                {
                    if (scaffoldLineList[i].HasColumn)
                        scaffoldOverhangBeamList.Add(new ScaffoldOverhangBeam(scaffoldCornerList[i], scaffoldLineList[i].Direction));
                    else
                        scaffoldOverhangBeamList.Add(new ScaffoldOverhangBeam(scaffoldCornerList[i], scaffoldLineList[iLast].Direction));
                }
            }

            /* 处理一般立杆点悬挑梁 */
            for (int i = 0; i < scaffoldLineList.Count; i++)
            {
                scaffoldOverhangBeamList.Add(new ScaffoldOverhangBeam(isScaffoldColumnHasBeamUnder[i], scaffoldLineList[i]));
            }
        }

        public List<XYZ> GetScaffoldAllColumns()
        {
            List<XYZ> result = new List<XYZ>();
            scaffoldLineList.ForEach(line => result.AddRange(line.GetPoints()));
            scaffoldCornerList.ForEach(corner => result.AddRange(corner.GetPoints()));
            return result;
        }

        /// <summary>
        /// 返回的横杆已基于XY平面避让立杆，基于立面避让相邻的横杆，返回的横杆位于常规模型顶部平面
        /// </summary>
        /// <returns></returns>
        public List<Curve> GetScaffoldAllHorizontalRods()
        {
            List<Curve> result = new List<Curve>();

            /* 所有横杆基于立面避让相邻的横杆
               连接横杆基于大横杆往下避让 */
            for (int i = 0; i < scaffoldRowList.Count - 1 && i < scaffoldConnectList.Count - 1; i++)
            {
                double offset = Global.GetNextOffset();
                if (offset > 0)
                {
                    result.AddRange(scaffoldRowList[i].GetCurves().Select(c => c.CreateOffset(offset, new XYZ(c.Direction.Y, -c.Direction.X, 0))).ToList());
                    result.AddRange(scaffoldConnectList[i].GetCurves().Select(c => c.CreateOffset(offset + Global.D, new XYZ(c.Direction.Y, -c.Direction.X, 0))).ToList());
                }
                else
                {
                    result.AddRange(scaffoldRowList[i].GetCurves());
                    result.AddRange(scaffoldConnectList[i].GetCurves().Select(c => c.CreateOffset(Global.D, new XYZ(c.Direction.Y, -c.Direction.X, 0))).ToList());
                }
            }

            /* 处理建筑面数量为基数或偶数相邻面间横杆在立面上的避让 */
            if (scaffoldRowList.Count % 2 > 0)
            {
                result.AddRange(scaffoldRowList.Last().GetCurves().Select(c => c.CreateOffset(-Global.D, new XYZ(c.Direction.Y, -c.Direction.X, 0))).ToList());
                result.AddRange(scaffoldConnectList.Last().GetCurves());
            }
            else
            {
                result.AddRange(scaffoldRowList.Last().GetCurves());
                result.AddRange(scaffoldConnectList.Last().GetCurves().Select(c => c.CreateOffset(Global.D, new XYZ(c.Direction.Y, -c.Direction.X, 0))).ToList());
            }
            return result;
        }
        /// <summary>
        /// 返回的斜杆已基于立面间作避让调整
        /// </summary>
        /// <returns></returns>
        public List<Curve> GetScaffoldAllSlantRods()
        {
            List<Curve> result = new List<Curve>();
            scaffoldSlantRodList.ForEach(ssr => result.AddRange(ssr.GetCurves()));
            return result;
        }

        public List<ScaffoldFixture> GetScaffoldFixtures()
        {
            return scaffoldFixtureList;
        }
        /// <summary>
        /// 返回板件的Z轴高度已初始化
        /// </summary>
        /// <returns></returns>
        public CurveArray[][] GetScaffoldBoards()
        {
            CurveArray[][] result = new CurveArray[scaffoldLineList.Count][];
            for (int i = 0; i < scaffoldLineList.Count; i++)
            {
                result[i] = scaffoldBoardList[i].CurveArrays;
            }
            return result;
        }

        public List<Line> GetScaffoldOverhangBeams()
        {
            List<Line> result = new List<Line>();
            scaffoldOverhangBeamList.ForEach(sob => result.AddRange(sob.GetLines()));
            return result;
        }

        private double SureValidLength(int i, out XYZ pointS, out XYZ pointE)
        {
            XYZ point1 = gm.TopCurveList[i > 0 ? i - 1 : gm.TopCurveList.Count - 1].GetEndPoint(0);
            XYZ point2 = gm.TopCurveList[i].GetEndPoint(0);
            XYZ point3 = gm.TopCurveList[i].GetEndPoint(1);
            XYZ point4 = gm.TopCurveList[(i + 1) % gm.TopCurveList.Count].GetEndPoint(1);
            XYZ vector1 = (gm.TopCurveList[i] as Line).Direction;
            pointS = scaffoldCurves1[i].GetEndPoint(0);
            pointE = scaffoldCurves1[i].GetEndPoint(1);
            if (ScaffoldUtil.IsConcavePoint(point1, point2, point3))
                pointS = pointS + vector1 * Global.LGHJ;
            if (ScaffoldUtil.IsConcavePoint(point2, point3, point4))
                pointE = pointE - vector1 * Global.LGHJ;
            return pointS.DistanceTo(pointE);
        }

        private void SetScaffoldColumnHasBeamUnder(bool[] arrayToSet, int count, bool fromStart = true)
        {
            if (fromStart)
                for (int i = 0; i < count; i++)
                    arrayToSet[i] = true;
            else
                for (int i = 0; i < count; i++)
                    arrayToSet[arrayToSet.Length - i - 1] = true;
        }

        #region 悬挑梁

        protected class ScaffoldOverhangBeam
        {
            private List<Line> beamLines;

            /// <summary>
            /// 阳拐角处悬挑梁构造函数
            /// </summary>
            /// <param name="array2D_1"></param>
            /// <param name="array1D_1"></param>
            public ScaffoldOverhangBeam(XYZ[,] array2D_1, XYZ[] array1D_1)
            {
                beamLines = new List<Line>();

                XYZ vector1 = (array2D_1[1, 1] - array2D_1[1, 0]).Normalize();
                double length1 = (array2D_1[1, 0].DistanceTo(array2D_1[1, 1]) + Global.OVERHANG_BEAM_BEYOND_DISTANCE) * 1.5;
                XYZ pointE = array2D_1[1, 1] + vector1 * length1;
                XYZ pointE1 = array2D_1[1, 1] + vector1 * length1 * 1.1;
                beamLines.Add(BeamLineCreation(array2D_1[1, 0], pointE));

                if (array1D_1[0] != null)
                {
                    using (Line line1 = ScaffoldUtil.GetExtendLine(array1D_1[0], array2D_1[1, 0], Global.OVERHANG_BEAM_BEYOND_DISTANCE, Global.OVERHANG_BEAM_BEYOND_DISTANCE))
                    {
                        beamLines.Add(BeamLineCreation(array2D_1[0, 1], pointE1, line1, 0.1 * length1));
                        XYZ normal1 = new XYZ(-line1.Direction.Y, line1.Direction.X, 0);
                        beamLines.Add(line1.CreateOffset(Global.BOTTOM_OVERHANG_BEAM_HEIGHT, normal1) as Line);
                    }
                }

                if (array1D_1[1] != null)
                {
                    using (Line line2 = ScaffoldUtil.GetExtendLine(array2D_1[1, 0], array1D_1[1], Global.OVERHANG_BEAM_BEYOND_DISTANCE, Global.OVERHANG_BEAM_BEYOND_DISTANCE))
                    {
                        beamLines.Add(BeamLineCreation(array2D_1[2, 1], pointE1, line2, 0.1 * length1));
                        XYZ normal2 = new XYZ(-line2.Direction.Y, line2.Direction.X, 0);
                        beamLines.Add(line2.CreateOffset(Global.BOTTOM_OVERHANG_BEAM_HEIGHT, normal2) as Line);
                    }
                }
            }

            /// <summary>
            /// 阴拐角悬挑梁构造函数
            /// </summary>
            /// <param name="scaffoldCorner"></param>
            /// <param name="direction"></param>
            public ScaffoldOverhangBeam(ScaffoldCorner scaffoldCorner, XYZ direction)
            {
                beamLines = new List<Line>();
                XYZ vector = new XYZ(direction.Y, -direction.X, 0);
                double length1 = (Global.LGHJ + Global.OVERHANG_BEAM_BEYOND_DISTANCE + Global.NJJQ) * 1.5;
                beamLines.Add(BeamLineCreation(scaffoldCorner.InnerPoint + vector * Global.LGHJ, scaffoldCorner.InnerPoint, length1));
                beamLines.Add(BeamLineCreation(scaffoldCorner.OuterPoint, scaffoldCorner.OuterPoint - vector * Global.LGHJ, length1));
            }

            /// <summary>
            /// 一般面悬挑梁构造函数
            /// </summary>
            /// <param name="isScaffoldColumnHasBeamUnder"></param>
            /// <param name="scaffoldColumn"></param>
            public ScaffoldOverhangBeam(bool[] isScaffoldColumnHasBeamUnder, ScaffoldColumn scaffoldColumn)
            {
                beamLines = new List<Line>();
                var innerPoints = scaffoldColumn.GetPoints(0);
                var outerPoints = scaffoldColumn.GetPoints(1);
                double length1 = (Global.LGHJ + Global.OVERHANG_BEAM_BEYOND_DISTANCE + Global.NJJQ) * 1.5;
                for (int i = 0; i < innerPoints.Count && i < outerPoints.Count; i++)
                {
                    if (!isScaffoldColumnHasBeamUnder[i])
                    {
                        beamLines.Add(BeamLineCreation(outerPoints[i], innerPoints[i], length1));
                    }
                }
            }

            private Line BeamLineCreation(XYZ pointS, XYZ pointE)
            {
                XYZ vector = (pointE - pointS).Normalize();
                return Line.CreateBound(pointS - vector * Global.OVERHANG_BEAM_BEYOND_DISTANCE, pointE - vector * (50 / 304.8));
            }

            private Line BeamLineCreation(XYZ pointS1, XYZ pointS2, double length)
            {
                XYZ vector = (pointS2 - pointS1).Normalize();
                return Line.CreateBound(pointS1 - vector * Global.OVERHANG_BEAM_BEYOND_DISTANCE, pointS2 + vector * length);
            }

            private Line BeamLineCreation(XYZ pointS, XYZ pointE, Line lineToReach, double endCut)
            {
                XYZ vector = (pointE - pointS).Normalize();
                IntersectionResultArray resultArray;
                Line.CreateBound(pointS - vector * 100, pointE).Intersect(lineToReach, out resultArray);
                return Line.CreateBound(resultArray.get_Item(0).XYZPoint - vector * Global.OVERHANG_BEAM_BEYOND_DISTANCE, pointE - vector * endCut);
            }

            public List<Line> GetLines()
            {
                return beamLines;
            }
        }

        #endregion
    }
}