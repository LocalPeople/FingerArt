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
        protected class ScaffoldConnect
        {
            private System.Collections.Generic.List<Line> connectCurves;

            public ScaffoldConnect(int index, System.Collections.Generic.List<ScaffoldColumn> scaffoldColumnList, System.Collections.Generic.List<ScaffoldCorner> scaffoldCornerList)
            {
                connectCurves = new System.Collections.Generic.List<Line>();
                if (!scaffoldCornerList[index].isRightAngle && !scaffoldCornerList[index].isOnly2Points)
                    connectCurves.Add(scaffoldCornerList[index].GetConnects(1));
                connectCurves.AddRange(scaffoldColumnList[index].GetConnects());
                if (!scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].isRightAngle)
                    connectCurves.Add(scaffoldCornerList[(index + 1) % scaffoldCornerList.Count].GetConnects(0));
            }
            /// <summary>
            /// 返回的连接横杆已基于XY平面避让立杆
            /// </summary>
            /// <returns></returns>
            public List<Line> GetCurves()
            {
                /* 返回连接横杆基于XY平面避让立杆 */
                return connectCurves.Select(c => c.CreateOffset(Global.D, XYZ.BasisZ) as Line).ToList();
            }
        }
    }
}
