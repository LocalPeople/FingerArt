using Autodesk.Revit.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XC.Util
{
    static class GeomUtil
    {
        /// <summary>
        /// 获取扩大后几何图形的线段集合
        /// </summary>
        /// <param name="curves">用于扩大的几何图形的线段集合</param>
        /// <param name="millimeter">扩大的毫米值</param>
        /// <returns>扩大后的新线段集合</returns>
        public static List<Curve> GetLargerCurves(IEnumerable curves, double millimeter)
        {
            double negative = IsClockWise(curves);
            List<Curve> curves2Intersect = new List<Curve>();
            List<ArcAtribute> resultIsArc = new List<ArcAtribute>();
            double offset = millimeter / 304.8;
            foreach (Curve c in curves)
            {
                if (c is Line)
                {
                    Line l = negative > -1 ? c as Line : c.CreateReversed() as Line;
                    XYZ verticalDirection = new XYZ(l.Direction.Y, -l.Direction.X, 0);
                    curves2Intersect.Add(Line.CreateBound(l.GetEndPoint(0) + verticalDirection * offset - l.Direction * (100),
                        l.GetEndPoint(1) + verticalDirection * offset + l.Direction * (100)));
                    resultIsArc.Add(new ArcAtribute(false));
                }
                else if (c is Arc)
                {
                    Arc a = negative > -1 ? c as Arc : c.CreateReversed() as Arc;
                    if (IsCenterInside(a.GetEndPoint(0), a.GetEndPoint(1), a.Center))
                    {
                        curves2Intersect.Add(Arc.Create(new Plane(a.Normal, a.Center), a.Radius + offset, 0, 2 * Math.PI));
                        resultIsArc.Add(new ArcAtribute(true, a.Center, a.Radius + offset));
                    }
                    else
                    {
                        if (offset < a.Radius)
                        {
                            curves2Intersect.Add(Arc.Create(new Plane(a.Normal, a.Center), a.Radius - offset, 0, 2 * Math.PI));
                            resultIsArc.Add(new ArcAtribute(true, a.Center, a.Radius - offset));
                        }
                    }
                }
            }
            List<XYZ> point2Curves = GetIntersectXyz(curves2Intersect);
            List<Curve> result = new List<Curve>();
            for (int i = 0; i < point2Curves.Count; i++)
            {
                if (resultIsArc[i].IsArc)
                {
                    result.Add(Arc.Create(point2Curves[i], point2Curves[(i + 1) % point2Curves.Count], GetPointOnArc(point2Curves[i], point2Curves[(i + 1) % point2Curves.Count], resultIsArc[i])));
                }
                else
                {
                    result.Add(Line.CreateBound(point2Curves[i], point2Curves[(i + 1) % point2Curves.Count]));
                }
            }
            return result;
        }
        /// <summary>
        /// 判断二维轮廓绘制方向 1：逆时针； -1：顺时针
        /// </summary>
        /// <param name="curves"></param>
        /// <returns>1：逆时针； -1：顺时针</returns>
        private static double IsClockWise(IEnumerable curves)
        {
            double initialLength = 0;
            double afterLength = 0;
            foreach (Curve c in curves)
            {
                initialLength += c.GetEndPoint(0).DistanceTo(c.GetEndPoint(1));
            }
            List<Curve> curves2Intersect = new List<Curve>();
            foreach (Curve c in curves)
            {
                XYZ horizontalDirection = (c.GetEndPoint(1) - c.GetEndPoint(0)).Normalize();
                XYZ verticalDirection = new XYZ(horizontalDirection.Y, -horizontalDirection.X, 0);
                curves2Intersect.Add(Line.CreateBound(c.GetEndPoint(0) + verticalDirection * 0.0328 - horizontalDirection * (5.0328),
                    c.GetEndPoint(1) + verticalDirection * 0.0328 + horizontalDirection * (5.0328)));
            }
            List<XYZ> point2Curves = GetIntersectXyz(curves2Intersect);
            for (int i = 0; i < point2Curves.Count; i++)
            {
                afterLength += point2Curves[i].DistanceTo(point2Curves[(i + 1) % point2Curves.Count]);
            }
            return initialLength < afterLength ? 1 : -1;
        }

        private static XYZ GetPointOnArc(XYZ end0, XYZ end1, ArcAtribute arcAtribute)
        {
            XYZ midPoint = (end0 + end1) * 0.5;
            XYZ direction = (midPoint - arcAtribute.Center).Normalize();
            return arcAtribute.Center + direction * arcAtribute.Radius;
        }

        private static List<XYZ> GetIntersectXyz(List<Curve> curves2Intersect)
        {
            List<XYZ> result = new List<XYZ>();
            for (int i = 0; i < curves2Intersect.Count; i++)
            {
                IntersectionResultArray ira;
                if (curves2Intersect[i].Intersect(curves2Intersect[i <= 0 ? curves2Intersect.Count - 1 : i - 1], out ira) == SetComparisonResult.Overlap)
                {
                    result.Add(ira.get_Item(0).XYZPoint);
                }
            }
            return result;
        }

        private static bool IsCenterInside(XYZ spoint, XYZ epoint, XYZ center)
        {
            XYZ spoint2Epoint = (epoint - spoint).Normalize();
            XYZ verticalDirection = new XYZ(spoint2Epoint.Y, -spoint2Epoint.X, 0);
            XYZ midPoint2circle = (center - (spoint + epoint) * 0.5).Normalize();
            return (verticalDirection + midPoint2circle).GetLength() < 0.01;
        }

        private class ArcAtribute
        {
            public bool IsArc { get; private set; }
            public XYZ Center { get; private set; }
            public double Radius { get; private set; }

            public ArcAtribute(bool isArc, XYZ center = null, double radius = 0.0)
            {
                IsArc = isArc;
                Center = center;
                Radius = radius;
            }
        }

        public static List<Curve> GetTopCurves(Solid solid, bool trfIsIdentity)
        {
            List<Curve> result = new List<Curve>();
            PlanarFace face = GetTopPlanarFace(solid, XYZ.BasisZ);
            Transform zTrf = trfIsIdentity ? Transform.Identity : Transform.CreateTranslation(new XYZ(0, 0, -face.Origin.Z));
            foreach (Edge e in face.EdgeLoops.get_Item(0))
            {
                result.Add(e.AsCurve().CreateTransformed(zTrf));
            }
            return result;
        }

        private static PlanarFace GetTopPlanarFace(Solid solid, XYZ vector)
        {
            List<PlanarFace> planarFaces = new List<PlanarFace>();
            foreach (Face face in solid.Faces)
            {
                if (face is PlanarFace)
                {
                    PlanarFace planarFace = face as PlanarFace;
                    if ((planarFace.FaceNormal - vector).GetLength() < 0.00328083989501312334)
                    {
                        planarFaces.Add(planarFace);
                    }
                }
            }
            return planarFaces.OrderByDescending(pf => pf.Origin.Z).First();
        }
    }
}
