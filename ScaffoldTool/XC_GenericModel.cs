using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace ScaffoldTool
{
    internal class XC_GenericModel
    {
        public double startElevation;
        public double endElevation;

        public List<Curve> TopCurveList { get; private set; }
        public List<Curve> BaseCurveList { get; private set; }
        public XC_ElementLevelSet LevelSet { get; private set; }

        public XC_GenericModel(FamilyInstance familyInstance)
        {
            foreach (GeometryObject geomObj in familyInstance.GetOriginalGeometry(new Options()))
            {
                using (Solid solid = geomObj as Solid)
                {
                    if (solid != null && solid.Volume > 0)
                    {
                        TopCurveList = GetTopCurves(solid);
                        BaseCurveList = GetBaseCurves(solid).Select(c => c.CreateReversed()).ToList();
                        break;
                    }
                }
            }
            startElevation = BaseCurveList[0].GetEndPoint(0).Z;
            endElevation = TopCurveList[0].GetEndPoint(0).Z;
            LevelSet = new XC_ElementLevelSet(familyInstance.Document, startElevation, endElevation);
        }

        private XC_GenericModel(Document doc, List<Curve> topCurves, List<Curve> baseCurves)
        {
            TopCurveList = topCurves;
            BaseCurveList = baseCurves;
            startElevation = BaseCurveList[0].GetEndPoint(0).Z;
            endElevation = TopCurveList[0].GetEndPoint(0).Z;
            LevelSet = new XC_ElementLevelSet(doc, startElevation, endElevation);
        }

        internal IEnumerable<XC_GenericModel> Devide(Document doc, double averageHeight)
        {
            double newStartElevation = startElevation;
            for (int i = 1; i < LevelSet.Length; i++)
            {
                if (LevelSet[i].Elevation - newStartElevation > averageHeight)
                {
                    double newEndElevation = LevelSet[i - 1].Elevation - Global.OVERHANG_EVERY_SECTION_OFFSET;
                    using (Transform topTrf = Transform.CreateTranslation(new XYZ(0, 0, newEndElevation - endElevation)),
                            baseTrf = Transform.CreateTranslation(new XYZ(0, 0, newStartElevation - startElevation)))
                    {
                        List<Curve> topCurves = TopCurveList.Select(c => c.CreateTransformed(topTrf)).ToList();
                        List<Curve> baseCurves = BaseCurveList.Select(c => c.CreateTransformed(baseTrf)).ToList();
                        yield return new XC_GenericModel(doc, topCurves, baseCurves);

                        newStartElevation = LevelSet[--i].Elevation;
                    }
                }
            }
            using (Transform baseTrf = Transform.CreateTranslation(new XYZ(0, 0, newStartElevation - startElevation)))
            {
                List<Curve> baseCurves = BaseCurveList.Select(c => c.CreateTransformed(baseTrf)).ToList();
                yield return new XC_GenericModel(doc, TopCurveList, baseCurves);
            }
            yield break;
        }

        #region 私有函数
        protected List<Curve> GetTopCurves(Solid solid)
        {
            List<Curve> result = new List<Curve>();
            PlanarFace face = GetPlanarFace(solid, XYZ.BasisZ, true);
            foreach (Edge e in face.EdgeLoops.get_Item(0))
            {
                result.Add(e.AsCurve());
            }
            return result;
        }

        protected List<Curve> GetBaseCurves(Solid solid)
        {
            List<Curve> result = new List<Curve>();
            PlanarFace face = GetPlanarFace(solid, -XYZ.BasisZ, true);
            foreach (Edge e in face.EdgeLoops.get_Item(0))
            {
                result.Add(e.AsCurve());
            }
            return result;
        }

        protected PlanarFace GetPlanarFace(Solid solid, XYZ vector, bool isBiggest)
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
            if (isBiggest)
                return planarFaces.OrderByDescending(pf => pf.Area).First();
            else
                return planarFaces.OrderBy(pf => pf.Area).First();
        }
        #endregion
    }

    internal class XC_ElementLevelSet : IEnumerable<Level>
    {
        public double Base2BaseOffset { get; private set; }
        public double Top2BaseOffset { get; private set; }
        public double Height { get; private set; }
        public Level this[int index]
        {
            get { return _list[index]; }
        }
        public int Length { get { return _list.Count; } }

        private List<Level> _list = new List<Level>();
        public XC_ElementLevelSet(Document doc, double start, double end)
        {
            foreach (var item in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(elem => elem.Elevation))
            {
                if (item.Elevation >= end)
                    break;
                if (item.Elevation >= start - 1)
                {
                    _list.Add(item);
                }
            }
            if (_list.Count <= 0)
                throw new Exception("缺少必要标高");
            Base2BaseOffset = start - _list[0].Elevation;
            Top2BaseOffset = end - _list[0].Elevation;
            Height = end - start;
        }

        public IEnumerator<Level> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}