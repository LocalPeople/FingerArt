using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScaffoldTool.ScaffoldComponent;

namespace ScaffoldTool
{
    partial class XC_KJScaffold
    {
        protected class ScaffoldCorner
        {
            private XYZ[] locationPoints1;
            private XYZ[] locationPoints2;
            public bool isConcave;
            public bool isOnly2Points;
            public bool isRightAngle;
            public XYZ[] Other2Points { get { return locationPoints2; } }
            public XYZ InnerPoint { get; private set; }
            public XYZ OuterPoint { get; private set; }

            public ScaffoldCorner(XYZ point1, XYZ point2, XYZ point3, XYZ point4, bool isConcave, bool isRightAngle)
            {
                locationPoints1 = new XYZ[4] { point1, point2, point3, point4 };
                locationPoints2 = new XYZ[2] { point3, point4 };
                InnerPoint = point1;
                OuterPoint = point2;
                this.isConcave = isConcave;
                this.isOnly2Points = false;
                this.isRightAngle = isRightAngle;
            }

            public ScaffoldCorner(XYZ point1, XYZ point2, bool isRightAngle)
            {
                locationPoints1 = new XYZ[2] { point1, point2 };
                locationPoints2 = new XYZ[0];
                InnerPoint = point1;
                OuterPoint = point2;
                this.isConcave = true;
                this.isOnly2Points = true;
                this.isRightAngle = isRightAngle;
            }

            public XYZ[] GetPoints()// 获取边角所有立杆点
            {
                return locationPoints1;
            }

            public Line GetConnects(int index)// 获取小横杆线段
            {
                if (index < 0 || index > 1)
                {
                    return null;
                }
                if (isOnly2Points)
                {
                    return Line.CreateBound(InnerPoint, OuterPoint);
                }
                return isConcave ? ScaffoldUtil.GetExtendLine(locationPoints2[index], OuterPoint, Global.ROW_BEYOND_DISTANCE, Global.ROW_BEYOND_DISTANCE)
                    : ScaffoldUtil.GetExtendLine(InnerPoint, locationPoints2[index], Global.ROW_BEYOND_DISTANCE, Global.ROW_BEYOND_DISTANCE);// 小横杆两端延长100毫米
            }

            public XYZ[] GetEndPoints(int index)
            {
                if (index < 0 || index > 1)
                {
                    return null;
                }
                return isConcave ? new XYZ[] { OuterPoint } :
                    (index > 0 ? new XYZ[] { OuterPoint, locationPoints2[index] } :
                    new XYZ[] { locationPoints2[index], OuterPoint });
            }

            /// <summary>
            /// 获取剪切确认中部横杆的线段集合
            /// </summary>
            /// <param name="index">0:用于横杆起点 1:用于横杆终点</param>
            /// <returns></returns>
            public Line[] GetScaffoldRowCut(int index)
            {
                if (isOnly2Points)
                {
                    using (Line diagonal = Line.CreateBound(InnerPoint, OuterPoint))
                        return new Line[] { diagonal.CreateOffset(Global.D, XYZ.BasisZ) as Line, null };
                }
                Line line1, line2;
                if (isConcave)
                {
                    line1 = Line.CreateBound(OuterPoint, Other2Points[0]);
                    line2 = Line.CreateBound(Other2Points[0], InnerPoint);
                }
                else
                {
                    line1 = isRightAngle ? Line.CreateBound(Other2Points[0], InnerPoint) : Line.CreateBound(InnerPoint, Other2Points[0]);
                    line2 = Line.CreateBound(OuterPoint, Other2Points[0]);
                }
                return index > 0 ?
                    new Line[] { line1 } :
                    new Line[] { line1, line2 };
            }
        }
    }
}
