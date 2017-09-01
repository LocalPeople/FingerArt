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
    /// 盘扣式脚手架数据计算类
    /// </summary>
    internal class XC_PKScaffold : ScaffoldBase
    {
        private Document doc;
        private XC_GenericModel gm;
        /// <summary>
        /// 调整后外轮廓线段（可能非闭合）
        /// </summary>
        public List<ScaffoldNewLine> scaffoldCurves2;
        public List<Line> scaffoldCurves1;
        private List<ScaffoldColumn> scaffoldLineList;
        /// <summary>
        /// 对应scaffoldLineList的结尾角点对象集合
        /// </summary>
        private ScaffoldCorner[] scaffoldCornerList;
        private List<ScaffoldConnect> scaffoldConnectList;
        private List<ScaffoldSlantRod> scaffoldSlantRodList;
        private List<ScaffoldBoard> scaffoldBoardList;
        private List<ScaffoldFixture> scaffoldFixtureList;

        public XC_GenericModel HostGenericModel { get { return gm; } }

        public XC_PKScaffold(Document doc, XC_GenericModel gm)
        {
            this.doc = doc;
            this.gm = gm;
            scaffoldCurves2 = new List<ScaffoldNewLine>();
            scaffoldCurves1 = new List<Line>();
            GeomUtil.GetLargerCurves(gm.TopCurveList, Global.NJJQ * 304.8).ForEach(c => scaffoldCurves1.Add(c as Line));
            List<XYZ> scaffoldPoints1 = scaffoldCurves1.Select(c => c.GetEndPoint(0)).ToList();// 轮廓顶点
            for (int i = 0; i < scaffoldPoints1.Count - 1; i++)
            {
                int iNext = (i + 1) % scaffoldPoints1.Count;
                int iNextTwo = (i + 2) % scaffoldPoints1.Count;
                int iNextThree = (i + 3) % scaffoldPoints1.Count;
                if (Math.Abs(scaffoldCurves1[i].Direction.DotProduct(scaffoldCurves1[iNext].Direction)) < 0.001)// 下一个拐角为90°
                {
                    bool isPointOneConcave = false, isPointThreeConcave = false;
                    double difference1 = scaffoldPoints1[i].DistanceTo(scaffoldPoints1[iNext]) % (300 / 304.8);// 取余调整
                    double adjustment = difference1 >= (150 / 304.8) ? (300 / 304.8) - difference1 : -difference1;// 获取调整距离
                    if (ScaffoldUtil.IsConcavePoint(scaffoldPoints1[i], scaffoldPoints1[iNext], scaffoldPoints1[iNextTwo]))
                        isPointOneConcave = true;
                    scaffoldPoints1[iNext] += scaffoldCurves1[i].Direction * adjustment;// 调整下一点
                    double angle1 = scaffoldCurves1[i].Direction.AngleTo(scaffoldCurves1[iNextTwo].Direction);
                    if (angle1 > 0.001745328366 && angle1 < 3.139847325224)
                    {
                        if (angle1 > Math.PI * 0.5)
                            angle1 -= Math.PI * 0.5;
                        adjustment /= Math.Cos(angle1);
                    }
                    if (ScaffoldUtil.IsConcavePoint(scaffoldPoints1[iNext], scaffoldPoints1[iNextTwo], scaffoldPoints1[iNextThree]))// 调整下二点
                        isPointThreeConcave = true;
                    if (isPointOneConcave == isPointThreeConcave)
                        scaffoldPoints1[iNextTwo] -= scaffoldCurves1[iNextTwo].Direction * adjustment;
                    else
                        scaffoldPoints1[iNextTwo] += scaffoldCurves1[iNextTwo].Direction * adjustment;
                    scaffoldCurves2.Add(new ScaffoldNewLine()
                    {
                        Line = Line.CreateBound(scaffoldPoints1[i], scaffoldPoints1[iNext]),
                        IsEndRightAngle = true,
                        IsEndConcave = isPointOneConcave
                    });
                }
                else
                {
                    double difference2 = scaffoldPoints1[i].DistanceTo(scaffoldPoints1[iNext]) % (300 / 304.8);// 取余调整
                    scaffoldCurves2.Add(new ScaffoldNewLine()
                    {
                        Line = Line.CreateBound(scaffoldPoints1[i], scaffoldPoints1[iNext] - scaffoldCurves1[i].Direction * difference2),
                        IsEndRightAngle = false,
                        IsEndConcave = ScaffoldUtil.IsConcavePoint(scaffoldPoints1[i], scaffoldPoints1[iNext], scaffoldPoints1[iNextTwo])
                    });
                }
            }
            XYZ vector1 = (scaffoldPoints1[0] - scaffoldPoints1[scaffoldPoints1.Count - 1]).Normalize();
            XYZ vector2 = (scaffoldPoints1[1] - scaffoldPoints1[0]).Normalize();
            double distance1 = scaffoldPoints1[scaffoldPoints1.Count - 1].DistanceTo(scaffoldPoints1[0])
                * vector1.DotProduct(scaffoldCurves1[scaffoldCurves1.Count - 1].Direction);
            double difference3 = distance1 % (300 / 304.8);
            Line line1 = difference3 < 0.003280839895 || difference3 > 0.980971128609 ?
                Line.CreateBound(scaffoldPoints1[scaffoldPoints1.Count - 1], scaffoldPoints1[scaffoldPoints1.Count - 1] + scaffoldCurves1[scaffoldCurves1.Count - 1].Direction * distance1) :
                Line.CreateBound(scaffoldPoints1[scaffoldPoints1.Count - 1], scaffoldPoints1[scaffoldPoints1.Count - 1] + scaffoldCurves1[scaffoldCurves1.Count - 1].Direction * (distance1 - difference3));
            scaffoldCurves2.Add(new ScaffoldNewLine()// 最后一边
            {
                Line = line1,
                IsEndRightAngle = Math.Abs(vector1.DotProduct(vector2)) < 0.001 && (difference3 < 0.003280839895 || difference3 > 0.980971128609),
                IsEndConcave = ScaffoldUtil.IsConcavePoint(scaffoldPoints1[scaffoldPoints1.Count - 1], scaffoldPoints1[0], scaffoldPoints1[1])
            });
        }

        #region 盘扣式内排立杆轮廓线对象

        public class ScaffoldNewLine
        {
            public Line Line { get; set; }
            public bool IsEndRightAngle { get; set; }
            public bool IsEndConcave { get; set; }
        }

        #endregion

        #region 初始化构件集合

        public void CalculateLines()
        {
            scaffoldLineList = new List<ScaffoldColumn>();
            for (int i = 0; i < scaffoldCurves2.Count; i++)
            {
                XYZ pointS, pointE;
                bool isStartNeedAdd, isEndNeedAdd;
                double validLength = SureValidLength(i, out pointS, out pointE, out isStartNeedAdd, out isEndNeedAdd);
                scaffoldLineList.Add(new ScaffoldColumn(validLength, pointS, pointE, isStartNeedAdd, isEndNeedAdd));
            }
        }

        private double SureValidLength(int i, out XYZ pointS, out XYZ pointE, out bool isStartNeedAdd, out bool isEndNeedAdd)
        {
            pointS = scaffoldCurves2[i].Line.GetEndPoint(0);
            pointE = scaffoldCurves2[i].Line.GetEndPoint(1);
            double length = pointS.DistanceTo(pointE);
            int iLast = i > 0 ? i - 1 : scaffoldCurves2.Count - 1;

            /* 起点阴角 */
            if (scaffoldCurves2[iLast].IsEndConcave)
            {
                if (scaffoldCurves2[iLast].IsEndRightAngle)
                {
                    pointS += scaffoldCurves2[i].Line.Direction * Global.LGHJ;
                    length -= Global.LGHJ;
                    isStartNeedAdd = false;
                }
                else
                {
                    pointS += scaffoldCurves2[i].Line.Direction * (300 / 304.8);
                    length -= (300 / 304.8);
                    isStartNeedAdd = true;
                }
            }

            /* 起点仰角 */
            else
            {
                if (scaffoldCurves2[iLast].IsEndRightAngle)
                    isStartNeedAdd = false;
                else
                {
                    pointS += scaffoldCurves2[i].Line.Direction * (300 / 304.8);
                    length -= (300 / 304.8);
                    isStartNeedAdd = true;
                }
            }

            /* 终点阴角 */
            if (scaffoldCurves2[i].IsEndConcave)
            {
                if (scaffoldCurves2[i].IsEndRightAngle)
                {
                    pointE -= scaffoldCurves2[i].Line.Direction * Global.LGHJ;
                    length -= Global.LGHJ;
                    isEndNeedAdd = false;
                }
                else
                {
                    pointE -= scaffoldCurves2[i].Line.Direction * (300 / 304.8);
                    length -= (300 / 304.8);
                    isEndNeedAdd = true;
                }
            }

            /* 终点仰角 */
            else
            {
                if (scaffoldCurves2[i].IsEndRightAngle)
                    isEndNeedAdd = false;
                else
                    isEndNeedAdd = true;
            }
            if (length > 0.1)
                return pointS.DistanceTo(pointE) - 100 / 304.8;
            else
                return -1;
        }

        public void CalculateCorners()
        {
            scaffoldCornerList = new ScaffoldCorner[scaffoldCurves2.Count];
            for (int i = 0; i < scaffoldCurves2.Count; i++)
            {
                scaffoldCornerList[i] = new ScaffoldCorner(scaffoldCurves2[i], scaffoldLineList[i], scaffoldLineList[(i + 1) % scaffoldLineList.Count]);
            }
            for (int i = 0; i < scaffoldCornerList.Length; i++)
            {
                if (scaffoldCornerList[i].DistanceTo(scaffoldCornerList[(i + 1) % scaffoldCornerList.Length]) <= Global.LGHJ)
                {
                    scaffoldCornerList[i] = new ScaffoldCorner(scaffoldLineList[i],
                        scaffoldLineList[(i + 2) % scaffoldLineList.Count], false);

                    scaffoldCornerList[(i + 1) % scaffoldCornerList.Length] = new ScaffoldCorner(scaffoldLineList[i],
                        scaffoldLineList[(i + 2) % scaffoldLineList.Count], true);
                    i++;
                }
            }
        }

        public void CalculateConnects()
        {
            scaffoldConnectList = new List<ScaffoldConnect>();
            for (int i = 0; i < scaffoldLineList.Count && i < scaffoldCornerList.Length; i++)
            {
                scaffoldConnectList.Add(new ScaffoldConnect(scaffoldLineList[i], scaffoldCornerList[i]));
            }
        }

        public void CalculateSlantRods()
        {
            scaffoldSlantRodList = new List<ScaffoldSlantRod>();
            for (int i = 0; i < scaffoldLineList.Count; i++)
            {
                scaffoldSlantRodList.Add(new ScaffoldSlantRod(i, scaffoldCornerList, scaffoldLineList));
            }
        }

        public void CalculateBoards(double bottomOffset)
        {
            scaffoldBoardList = new List<ScaffoldBoard>();
            ScaffoldBoardBuilder builder = new ScaffoldBoardBuilder(
                gm.startElevation + Global.PIPE_BASE_OFFSET + 0.5 * Global.D + bottomOffset,
                Global.SCAFFOLD_NET_THICK,
                Global.SCAFFOLD_VERTICAL_FLOOR_THICK,
                Global.D);
            for (int i = 0; i < scaffoldCornerList.Length; i++)
            {
                builder.SetBoardPoints(scaffoldCornerList[i].InnerPoint(false), scaffoldCornerList[i].OuterPoint(false),
                    scaffoldCornerList[(i + 1) % scaffoldCornerList.Length].InnerPoint(true), scaffoldCornerList[(i + 1) % scaffoldCornerList.Length].OuterPoint(true));
                scaffoldBoardList.Add(builder.Create());
            }
        }

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

        #endregion

        #region 构件集合获取

        public List<XYZ> GetScaffoldAllColumns()
        {
            List<XYZ> result = new List<XYZ>();
            scaffoldLineList.ForEach(line => result.AddRange(line.GetPoints()));
            for (int i = 0; i < scaffoldCornerList.Length; i++)
            {
                if (!scaffoldCornerList[i].IsRepeat)
                {
                    result.AddRange(scaffoldCornerList[i].GetPoints());
                }
            }
            return result;
        }

        public List<Line> GetScaffoldAllHorizontalRods()
        {
            List<Line> result = new List<Line>();
            scaffoldConnectList.ForEach(connect => result.AddRange(connect.GetCurves()));
            return result;
        }

        public List<Line> GetScaffoldAllSlantRods()
        {
            List<Line> result = new List<Line>();
            scaffoldSlantRodList.ForEach(ssr => result.AddRange(ssr.GetCurves()));
            return result;
        }

        public CurveArray[][] GetScaffoldBoards()
        {
            CurveArray[][] result = new CurveArray[scaffoldLineList.Count][];
            for (int i = 0; i < scaffoldLineList.Count; i++)
            {
                result[i] = scaffoldBoardList[i].CurveArrays;
            }
            return result;
        }

        public List<ScaffoldFixture> GetScaffoldFixtures()
        {
            return scaffoldFixtureList;
        }

        #endregion

        protected class ScaffoldColumn
        {
            public List<XYZ> locationPoints1;
            public List<XYZ> locationPoints2;
            public XYZ Direction { get; private set; }
            internal XYZ[] startPoints;
            internal XYZ[] endPoints;

            public ScaffoldColumn(double validLength, XYZ pointS, XYZ pointE, bool isStartNeedAdd, bool isEndNeedAdd)
            {
                locationPoints1 = new List<XYZ>();
                locationPoints2 = new List<XYZ>();
                if (validLength == -1)
                    return;
                Direction = (pointE - pointS).Normalize();
                XYZ vector2 = new XYZ(Direction.Y, -Direction.X, 0);
                /* 边太小，只考虑起点和终点 */
                if (validLength <= Global.LGZJ)
                {
                    if (isStartNeedAdd)
                        locationPoints1.Add(pointS);
                    else
                        startPoints = new XYZ[] { pointS, pointS + vector2 * Global.LGHJ };
                    if (isEndNeedAdd)
                        locationPoints1.Add(pointE);
                    else
                        endPoints = new XYZ[] { pointE, pointE + vector2 * Global.LGHJ };
                }
                else
                {
                    if (isStartNeedAdd)
                        locationPoints1.Add(pointS);
                    else
                        startPoints = new XYZ[] { pointS, pointS + vector2 * Global.LGHJ };
                    int num1 = (int)(validLength / Global.LGZJ);
                    for (int i = 0; i < num1; i++)
                    {
                        locationPoints1.Add(pointS + Direction * Global.LGZJ * (i + 1));
                        validLength -= Global.LGZJ;
                    }
                    if (validLength <= 0 && !isEndNeedAdd)
                        locationPoints1.RemoveAt(locationPoints1.Count - 1);
                    if (isEndNeedAdd)
                        locationPoints1.Add(pointE);
                    else
                        endPoints = new XYZ[] { pointE, pointE + vector2 * Global.LGHJ };
                }
                locationPoints2.AddRange(locationPoints1.Select(p => p + vector2 * Global.LGHJ));
            }

            public List<XYZ> GetPoints()
            {
                return locationPoints1.Concat(locationPoints2).ToList();
            }

            public List<XYZ> GetPoints(int index)
            {
                return index > 0 ? locationPoints2 : locationPoints1;
            }

            public List<Line> GetConnects()
            {
                List<Line> result = new List<Line>();
                for (int i = 0; i < locationPoints1.Count && i < locationPoints2.Count; i++)
                {
                    result.Add(Line.CreateBound(locationPoints1[i], locationPoints2[i]));
                    if (i > 0)
                    {
                        result.Add(Line.CreateBound(locationPoints1[i - 1], locationPoints1[i]));
                        result.Add(Line.CreateBound(locationPoints2[i - 1], locationPoints2[i]));
                    }
                }
                if (locationPoints1.Count > 0)
                {
                    if (startPoints != null)
                    {
                        result.Add(Line.CreateBound(startPoints[0], locationPoints1[0]));
                        result.Add(Line.CreateBound(startPoints[1], locationPoints2[0]));
                    }
                    if (endPoints != null)
                    {
                        result.Add(Line.CreateBound(locationPoints1.Last(), endPoints[0]));
                        result.Add(Line.CreateBound(locationPoints2.Last(), endPoints[1]));
                    }
                }
                else
                {
                    if (startPoints != null && endPoints != null)
                    {
                        result.Add(Line.CreateBound(startPoints[0], endPoints[0]));
                        result.Add(Line.CreateBound(startPoints[1], endPoints[1]));
                    }
                }
                return result;
            }
        }

        private class ScaffoldCorner
        {
            private XYZ[] locationPoints1;
            private bool isConcave;
            private XYZ[] virtualStartPoints, virtualEndPoints;

            public enum CornerType
            {
                Square,
                Polygon4Point,
                Polygon5Point
            }

            public CornerType IsStandardColumn { get; private set; }
            public bool IsRepeat { get; private set; }
            public XYZ InnerPoint(bool head)
            {
                switch (IsStandardColumn)
                {
                    case CornerType.Square:
                        return locationPoints1[0];
                    case CornerType.Polygon5Point:
                        return head ? virtualStartPoints[0] : virtualEndPoints[0];
                    case CornerType.Polygon4Point:
                        return IsRepeat ? locationPoints1[3] : locationPoints1[0];
                    default:
                        return null;
                }
            }

            public XYZ OuterPoint(bool head)
            {
                switch (IsStandardColumn)
                {
                    case CornerType.Square:
                        return locationPoints1[2];
                    case CornerType.Polygon5Point:
                        return head ? virtualStartPoints[1] : virtualEndPoints[1];
                    case CornerType.Polygon4Point:
                        return IsRepeat ? locationPoints1[2] : locationPoints1[1];
                    default:
                        return null;
                }
            }

            public double DistanceTo(ScaffoldCorner corner)
            {
                return locationPoints1[0].DistanceTo(corner.locationPoints1[0]);
            }

            public ScaffoldCorner(ScaffoldNewLine scaffoldNewLine, ScaffoldColumn thisLineColumn, ScaffoldColumn nextLineColumn)
            {
                IsRepeat = false;
                if (scaffoldNewLine.IsEndRightAngle)
                {
                    IsStandardColumn = CornerType.Square;
                    XYZ vector1 = new XYZ(scaffoldNewLine.Line.Direction.Y, -scaffoldNewLine.Line.Direction.X, 0);
                    isConcave = scaffoldNewLine.IsEndConcave;
                    locationPoints1 = new XYZ[4];
                    locationPoints1[0] = scaffoldNewLine.Line.GetEndPoint(1);
                    if (scaffoldNewLine.IsEndConcave)
                        locationPoints1[1] = locationPoints1[0] - scaffoldNewLine.Line.Direction * Global.LGHJ;
                    else
                        locationPoints1[1] = locationPoints1[0] + scaffoldNewLine.Line.Direction * Global.LGHJ;
                    locationPoints1[2] = locationPoints1[1] + vector1 * Global.LGHJ;
                    locationPoints1[3] = locationPoints1[0] + vector1 * Global.LGHJ;
                }
                else
                {
                    virtualStartPoints = new XYZ[] { thisLineColumn.locationPoints1.Last(), thisLineColumn.locationPoints2.Last() };
                    virtualEndPoints = new XYZ[] { nextLineColumn.locationPoints1.First(), nextLineColumn.locationPoints2.First() };
                    IsStandardColumn = CornerType.Polygon5Point;
                    isConcave = scaffoldNewLine.IsEndConcave;
                    IntersectionResultArray resultArray;
                    if (isConcave)
                        Line.CreateBound(virtualStartPoints[0], virtualStartPoints[0] + thisLineColumn.Direction * 100)
                            .Intersect
                            (Line.CreateBound(virtualEndPoints[0], virtualEndPoints[0] - nextLineColumn.Direction * 100), out resultArray);
                    else
                        Line.CreateBound(virtualStartPoints[1], virtualStartPoints[1] + thisLineColumn.Direction * 100)
                            .Intersect
                            (Line.CreateBound(virtualEndPoints[1], virtualEndPoints[1] - nextLineColumn.Direction * 100), out resultArray);
                    locationPoints1 = new XYZ[] { resultArray.get_Item(0).XYZPoint };
                }
            }

            public ScaffoldCorner(ScaffoldColumn scaffoldColumn1, ScaffoldColumn scaffoldColumn2, bool isRepeat)
            {
                IsRepeat = isRepeat;
                IsStandardColumn = CornerType.Polygon4Point;
                locationPoints1 = new XYZ[4];
                if (scaffoldColumn1.endPoints != null)
                {
                    locationPoints1[0] = scaffoldColumn1.endPoints[0];
                    locationPoints1[1] = scaffoldColumn1.endPoints[1];
                }
                if (scaffoldColumn2.startPoints != null)
                {
                    locationPoints1[2] = scaffoldColumn2.startPoints[1];
                    locationPoints1[3] = scaffoldColumn2.startPoints[0];
                }
            }

            public XYZ[] GetPoints()
            {
                return locationPoints1;
            }

            public XYZ[] GetOuterPoints(int index)
            {
                if (isConcave)
                    return new XYZ[] { locationPoints1[2] };
                if (IsStandardColumn == CornerType.Polygon4Point)
                    return index > 0 ?
                        new XYZ[] { locationPoints1[1] } :
                        new XYZ[] { locationPoints1[2] };
                return index > 0 ?
                    new XYZ[] { locationPoints1[3], locationPoints1[2] } :
                    new XYZ[] { locationPoints1[2], locationPoints1[1] };
            }

            public Line[] GetConnects()
            {
                Line[] result;

                /* 扣件式连接 */
                if (IsStandardColumn == CornerType.Polygon5Point)
                {
                    result = new Line[3];
                    if (isConcave)
                    {
                        result[0] = Line.CreateBound(virtualStartPoints[1], virtualEndPoints[1]);
                        result[1] = Line.CreateBound(virtualStartPoints[0], locationPoints1[0]);
                        result[2] = Line.CreateBound(locationPoints1[0], virtualEndPoints[0]);
                    }
                    else
                    {
                        result[0] = Line.CreateBound(virtualStartPoints[0], virtualEndPoints[0]);
                        result[1] = Line.CreateBound(virtualStartPoints[1], locationPoints1[0]);
                        result[2] = Line.CreateBound(locationPoints1[0], virtualEndPoints[1]);
                    }
                }

                /* 盘扣式连接 */
                else
                {
                    result = new Line[4];
                    result[0] = Line.CreateBound(locationPoints1[0], locationPoints1[1]);
                    result[1] = Line.CreateBound(locationPoints1[1], locationPoints1[2]);
                    result[2] = Line.CreateBound(locationPoints1[2], locationPoints1[3]);
                    result[3] = Line.CreateBound(locationPoints1[3], locationPoints1[0]);
                }
                return result;
            }
        }

        private class ScaffoldConnect
        {
            private List<Line> connectCurves;

            public ScaffoldConnect(ScaffoldColumn scaffoldColumn, ScaffoldCorner scaffoldCorner)
            {
                connectCurves = new List<Line>();
                connectCurves.AddRange(scaffoldColumn.GetConnects());
                if (!scaffoldCorner.IsRepeat)
                    connectCurves.AddRange(scaffoldCorner.GetConnects());
            }

            public List<Line> GetCurves()
            {
                return connectCurves;
            }
        }

        private class ScaffoldSlantRod
        {
            private List<Line> slantRodLines;

            public ScaffoldSlantRod(int i, ScaffoldCorner[] scaffoldCornerList, List<ScaffoldColumn> scaffoldLineList)
            {
                slantRodLines = new List<Line>();
                List<XYZ> points = new List<XYZ>();
                int iLast = i > 0 ? i - 1 : scaffoldCornerList.Length - 1;
                if (scaffoldCornerList[i].IsRepeat)
                    return;
                if (scaffoldCornerList[iLast].IsStandardColumn != ScaffoldCorner.CornerType.Polygon5Point)
                    points.AddRange(scaffoldCornerList[iLast].GetOuterPoints(0));

                points.AddRange(scaffoldLineList[i].GetPoints(1));

                if (scaffoldCornerList[i].IsStandardColumn != ScaffoldCorner.CornerType.Polygon5Point)
                    points.AddRange(scaffoldCornerList[i].GetOuterPoints(1));

                /* 斜杆5跨对称放置 */
                bool leftDownRightup = true;
                for (int j = 0; j < points.Count - 2; j += 5)
                {
                    if (leftDownRightup)
                        slantRodLines.Add(Line.CreateBound(points[j], points[j + 1] + new XYZ(0, 0, Global.BJ)));
                    else
                        slantRodLines.Add(Line.CreateBound(points[j] + new XYZ(0, 0, Global.BJ), points[j + 1]));
                    leftDownRightup = !leftDownRightup;
                }
                if (leftDownRightup)
                    slantRodLines.Add(Line.CreateBound(points[points.Count - 2], points[points.Count - 1] + new XYZ(0, 0, Global.BJ)));
                else
                    slantRodLines.Add(Line.CreateBound(points[points.Count - 2] + new XYZ(0, 0, Global.BJ), points[points.Count - 1]));
            }

            public List<Line> GetCurves()
            {
                return slantRodLines;
            }
        }
    }
}
