using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScaffoldTool
{
    partial class XC_KJScaffold
    {
        protected class ScaffoldColumn
        {
            private XYZ endPoint;
            private XYZ startPoint;
            private double validLength;
            private List<XYZ> locationPoints1;
            private List<XYZ> locationPoints2;
            public XYZ Direction { get; private set; }
            public bool HasColumn { get; private set; }

            public ScaffoldColumn(double validLength, XYZ pointS, XYZ pointE)
            {
                this.validLength = validLength;
                this.startPoint = pointS;
                this.endPoint = pointE;
            }

            internal void SurePoints(double rowDistance, double columnDistance)
            {
                if (validLength < Global.LGZJ)// 边太小不创建立杆
                {
                    locationPoints1 = locationPoints2 = new List<XYZ>();
                    HasColumn = false;
                    return;
                }
                HasColumn = true;
                int num1 = (int)(validLength / columnDistance);
                double length1 = (validLength - columnDistance * num1) * 0.5;
                while (length1 < rowDistance)
                {
                    length1 = (validLength - columnDistance * --num1) * 0.5;
                }
                Direction = (endPoint - startPoint).Normalize();
                locationPoints1 = new List<XYZ>();
                locationPoints1.Add(startPoint + Direction * length1);
                for (int i = 1; i <= num1; i++)
                {
                    locationPoints1.Add(startPoint + Direction * (length1 + i * columnDistance));
                }
                XYZ vector2 = new XYZ(Direction.Y, -Direction.X, 0);
                locationPoints2 = locationPoints1.Select(p => p + vector2 * rowDistance).ToList();
            }

            internal List<Line> GetConnects()
            {
                List<Line> result = new List<Line>();
                if (locationPoints1.Count > 0 && locationPoints2.Count > 0)
                {
                    XYZ vector1 = (locationPoints2[0] - locationPoints1[0]).Normalize();
                    for (int i = 0; i < locationPoints1.Count && i < locationPoints2.Count; i++)
                    {
                        result.Add(Line.CreateBound(locationPoints1[i] - vector1 * Global.ROW_BEYOND_DISTANCE, locationPoints2[i] + vector1 * Global.ROW_BEYOND_DISTANCE));// 小横杆两端延长100毫米
                    }
                }
                return result;
            }

            public List<XYZ> GetPoints()
            {
                return locationPoints1.Concat(locationPoints2).ToList();
            }

            public List<XYZ> GetPoints(int index)
            {
                if (index < 0 || index > 1)
                {
                    return null;
                }
                return index > 0 ? locationPoints2 : locationPoints1;
            }
        }
    }
}
