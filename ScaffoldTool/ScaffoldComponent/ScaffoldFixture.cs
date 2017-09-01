using Autodesk.Revit.DB;
using System.Collections.Generic;
using XC.Util;
using System;

namespace ScaffoldTool.ScaffoldComponent
{
    //
    // 摘要：
    //     连墙件
    public class ScaffoldFixtureBuilder : IBuilder<IEnumerable<ScaffoldFixture>>
    {
        private readonly ElementCategoryFilter columnFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
        private readonly ElementCategoryFilter beamFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);
        private readonly ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
        private XYZ location, horizontal, vertical;
        private double solidHeight;
        private Document doc;

        public ScaffoldFixtureBuilder(Document doc, double solidHeight)
        {
            this.doc = doc;
            this.solidHeight = solidHeight;
        }

        public void SetVector(XYZ horizontal, XYZ vertical)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }

        public void SetLocation(XYZ location)
        {
            this.location = location;
        }

        private double DistanceToElement(Element elem, XYZ pointS, XYZ vector)
        {
            using (Line line = Line.CreateBound(new XYZ(pointS.X, pointS.Y, 0), new XYZ(pointS.X, pointS.Y, 0) + vector * 100))
            {
                foreach (GeometryObject geomObj in elem.get_Geometry(new Options()))
                    using (Solid soild = geomObj as Solid)
                        if (soild != null && soild.Volume != 0)
                        {
                            IntersectionResultArray resultArray;
                            double min = -1;
                            foreach (Curve c in GeomUtil.GetTopCurves(soild, false))
                            {
                                if (line.Intersect(c, out resultArray) == SetComparisonResult.Overlap)
                                {
                                    double distance = resultArray.get_Item(0).XYZPoint.DistanceTo(line.GetEndPoint(0));
                                    if (min <= 0 || distance < min)
                                        min = distance;
                                }
                            }
                            return min;
                        }
                return -1;
            }
        }

        public IEnumerable<ScaffoldFixture> Create()
        {
            using (CurveLoop solidLoop = new CurveLoop())
            {
                XYZ pointL = location + horizontal * Global.D / 2;
                XYZ pointR = location + horizontal * Global.D * 1.5;
                XYZ pointL1 = pointL + vertical * (Global.NJJQ + 200 / 304.8);// Solid往建筑内部延伸200毫米
                XYZ pointR1 = pointR + vertical * (Global.NJJQ + 200 / 304.8);
                solidLoop.Append(Line.CreateBound(pointL, pointR));
                solidLoop.Append(Line.CreateBound(pointR, pointR1));
                solidLoop.Append(Line.CreateBound(pointR1, pointL1));
                solidLoop.Append(Line.CreateBound(pointL1, pointL));
                using (Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { solidLoop }, XYZ.BasisZ, solidHeight))
                {
                    using (ElementIntersectsSolidFilter solidFilter = new ElementIntersectsSolidFilter(solid))
                    {
                        foreach (Element elem in new FilteredElementCollector(doc).WherePasses(solidFilter))
                        {
                            double distance;
                            if ((distance = DistanceToElement(elem, location, vertical)) > 0)
                            {
                                if (beamFilter.PassesFilter(elem))
                                    yield return new BeamFixture().Build(elem, location, vertical, distance);
                                else if (columnFilter.PassesFilter(elem))
                                    yield return new ColumnFixture().Build(elem, location, vertical, distance);
                                else if (wallFilter.PassesFilter(elem))
                                    yield return new WallFixture().Build(elem, location, vertical, distance);
                            }
                        }
                        yield break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 连墙件模型基类
    /// </summary>
    public abstract class ScaffoldFixture
    {
        protected readonly static double LEVEL_OFFSET = 150 / 304.8;// 标高处偏移150毫米

        public Line[] Curves { get; protected set; }// 建模线段
        public Level Level { get; protected set; }// 参照标高

        public abstract ScaffoldFixture Build(Element elem, XYZ pointS, XYZ vector, double distance2Element);
    }

    public class BeamFixture : ScaffoldFixture
    {
        public override ScaffoldFixture Build(Element elem, XYZ pointS, XYZ vector, double distance2Element)
        {
            Level = elem.Document.GetElement(elem.LookupParameter("参照标高").AsElementId()) as Level;
            Curves = new Line[2];
            pointS = new XYZ(pointS.X, pointS.Y, Level.Elevation + elem.LookupParameter("起点标高偏移").AsDouble() + LEVEL_OFFSET);// 标高处偏移150毫米
            XYZ pointM1 = pointS + vector * (distance2Element + 100 / 304.8);// 垂点往梁一侧伸入100毫米
            XYZ pointE = pointS + vector * (distance2Element + 200 / 304.8);// 杆件终点往梁一侧伸入200毫米
            using (Line line1 = Line.CreateBound(pointS - vector * (100 / 304.8), pointE))// 杆件向外预留100毫米
                Curves[0] = line1.CreateOffset(Global.D, XYZ.BasisZ) as Line;
            XYZ pointM2 = new XYZ(pointM1.X, pointM1.Y, Level.Elevation + elem.LookupParameter("起点标高偏移").AsDouble() - 200 / 304.8);// 结构梁表面下埋200毫米
            Curves[1] = Line.CreateBound(pointM2, pointM1 + XYZ.BasisZ * (100 / 304.8));// 杆件向上预留100毫米
            return this;
        }
    }

    public class ColumnFixture : ScaffoldFixture
    {
        public override ScaffoldFixture Build(Element elem, XYZ pointS, XYZ vector, double distance2Element)
        {
            Level = elem.Document.GetElement(elem.LookupParameter("底部标高").AsElementId()) as Level;
            Curves = new Line[1];
            pointS = new XYZ(pointS.X, pointS.Y, Level.Elevation + elem.LookupParameter("底部偏移").AsDouble() + LEVEL_OFFSET);// 标高处偏移150毫米
            XYZ pointE = pointS + vector * (distance2Element + 200 / 304.8);// 杆件终点往柱一侧伸入200毫米
            using (Line line1 = Line.CreateBound(pointS - vector * (100 / 304.8), pointE))// 杆件向外预留100毫米
                Curves[0] = line1.CreateOffset(Global.D, XYZ.BasisZ) as Line;
            return this;
        }
    }

    public class WallFixture : ScaffoldFixture
    {
        public override ScaffoldFixture Build(Element elem, XYZ pointS, XYZ vector, double distance2Element)
        {
            Level = elem.Document.GetElement(elem.LookupParameter("底部限制条件").AsElementId()) as Level;
            Curves = new Line[1];
            pointS = new XYZ(pointS.X, pointS.Y, Level.Elevation + elem.LookupParameter("底部偏移").AsDouble() + LEVEL_OFFSET);// 标高处偏移150毫米
            XYZ pointE = pointS + vector * (distance2Element + 200 / 304.8);// 杆件终点往墙一侧伸入200毫米
            using (Line line1 = Line.CreateBound(pointS - vector * (100 / 304.8), pointE))// 杆件向外预留100毫米
                Curves[0] = line1.CreateOffset(Global.D, XYZ.BasisZ) as Line;
            return this;
        }
    }
}
