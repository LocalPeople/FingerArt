using Autodesk.Revit.DB;
using System;

namespace ScaffoldTool.ScaffoldComponent
{
    //
    // 摘要：
    //     脚手板、挡脚板、安全网
    public class ScaffoldBoardBuilder : IBuilder<ScaffoldBoard>
    {
        //    Inner
        // A|--------|C
        //  |        |
        // B|--------|D
        //    Outer
        private XYZ pointA, pointB, pointC, pointD;
        private double axisZ;
        private double netThick, baffleThick, pipeThick;

        public ScaffoldBoardBuilder(double axisZ, double netThick, double baffleThick, double pipeThick)
        {
            this.axisZ = axisZ;
            this.netThick = netThick;
            this.baffleThick = baffleThick;
            this.pipeThick = pipeThick;
        }

        /// <summary>
        /// 设置Board的四个定位点
        /// </summary>
        public void SetBoardPoints(XYZ pointA, XYZ pointB, XYZ pointC, XYZ pointD)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.pointC = pointC;
            this.pointD = pointD;
        }

        public ScaffoldBoard Create()
        {
            /* Board初始Z轴高度已初始化 */
            CurveArray[] curveArrays = new CurveArray[3];
            XYZ vector1 = (pointB - pointA).Normalize();
            XYZ vector2 = (pointD - pointC).Normalize();
            XYZ pointS1 = new XYZ(pointA.X + vector1.X * pipeThick, pointA.Y + vector1.Y * pipeThick, axisZ);
            XYZ pointS2 = new XYZ(pointB.X - vector1.X * pipeThick, pointB.Y - vector1.Y * pipeThick, axisZ);
            XYZ pointE1 = new XYZ(pointC.X + vector2.X * pipeThick, pointC.Y + vector2.Y * pipeThick, axisZ);
            XYZ pointE2 = new XYZ(pointD.X - vector2.X * pipeThick, pointD.Y - vector2.Y * pipeThick, axisZ);
            curveArrays[0] = new CurveArray();// 脚手板
            curveArrays[0].Append(Line.CreateBound(pointS1, pointS2));
            curveArrays[0].Append(Line.CreateBound(pointS2, pointE2));
            curveArrays[0].Append(Line.CreateBound(pointE2, pointE1));
            curveArrays[0].Append(Line.CreateBound(pointE1, pointS1));
            XYZ vector3 = (pointD - pointB).Normalize();
            XYZ vector4 = new XYZ(-vector3.Y, vector3.X, 0);
            pointS1 = new XYZ(pointS2.X + vector4.X * baffleThick * 0.5, pointS2.Y + vector4.Y * baffleThick * 0.5, axisZ);
            pointE1 = new XYZ(pointE2.X + vector4.X * baffleThick * 0.5, pointE2.Y + vector4.Y * baffleThick * 0.5, axisZ);
            curveArrays[1] = new CurveArray();// 挡脚板
            curveArrays[1].Append(Line.CreateBound(pointS1, pointE1));
            curveArrays[2] = new CurveArray();// 安全网
            double angle = vector1.AngleTo(vector3);
            double offsetS1 = (pipeThick + baffleThick + netThick * 0.5) / Math.Sin(angle);
            double offsetE1 = (pipeThick + baffleThick + netThick * 0.5) / Math.Sin(angle);
            pointS1 = new XYZ(pointB.X - vector1.X * offsetS1, pointB.Y - vector1.Y * offsetS1, 0);
            pointE1 = new XYZ(pointD.X - vector2.X * offsetE1, pointD.Y - vector2.Y * offsetE1, 0);
            curveArrays[2].Append(Line.CreateBound(pointS1, pointE1));
            return new ScaffoldBoard { CurveArrays = curveArrays };
        }
    }

    public class ScaffoldBoard
    {
        /// <summary>
        /// 返回Board的Z轴高度已初始化
        /// </summary>
        public CurveArray[] CurveArrays { get; internal set; }

        internal ScaffoldBoard() { }
    }
}
