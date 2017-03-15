using Autodesk.Revit.DB;
using ScaffoldTool.ScaffoldComponent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ScaffoldTool
{
    internal class KJScaffoldXmlHelper : ScaffoldXmlHelperBase
    {
        private readonly static string PathExtension = @"扣件式分层构件.xml";

        private KJScaffoldXmlHelper(Document doc)
        {
            _xmlPath = Path.ChangeExtension(doc.PathName, PathExtension);
        }

        public static KJScaffoldXmlHelper GetWritableXmlHelper(Document doc, bool overWrite = false)
        {
            KJScaffoldXmlHelper writableXml = new KJScaffoldXmlHelper(doc);
            if (overWrite)
                writableXml.CreateXml();
            else
                writableXml.LoadWritableXml();
            return writableXml;
        }

        public static KJScaffoldXmlHelper GetReadableXmlHelper(Document doc)
        {
            KJScaffoldXmlHelper readableXml = new KJScaffoldXmlHelper(doc);
            readableXml.LoadXml();
            return readableXml;
        }

        public override void CreateXml()
        {
            base.CreateXml();
            XmlNode nodeTop = _xmlDoc.SelectSingleNode("构件");
            XmlNode bottomPlateCollection = _xmlDoc.CreateElement("垫板集合");
            nodeTop.AppendChild(bottomPlateCollection);
            XmlNode overhangCollection = _xmlDoc.CreateElement("悬挑梁集合");
            nodeTop.AppendChild(overhangCollection);
        }

        #region 回退及重做
        public static void Undo(Document doc)
        {
            Undo(doc, PathExtension);
        }

        public static void Redone(Document doc)
        {
            Redone(doc, PathExtension);
        }

        public static void Clear(Document doc)
        {
            Clear(doc, PathExtension);
        }
        #endregion

        #region 分层模型记录
        public static void GetBuildInfo(Document doc, out List<string> alreadyBuilt, out List<string> notYetBuilt)
        {
            GetBuildInfo(doc, out alreadyBuilt, out notYetBuilt, PathExtension);
        }

        public static void UpdateBuildInfo(Document doc, IEnumerable<string> toBuild, IEnumerable<string> toDelete)
        {
            UpdateBuildInfo(doc, toBuild, toDelete, PathExtension);
        }
        #endregion

        #region 悬挑梁
        internal void WriteOverhangBeam(Curve beamLine, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("悬挑梁集合");
            XYZ startPoint = beamLine.GetEndPoint(0);
            XYZ endPoint = beamLine.GetEndPoint(1);
            var nodeElement = _xmlDoc.CreateElement("悬挑梁");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[0].Name);
            nodeCollection.AppendChild(nodeElement);
            var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");
            var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");
            nodeStartPoint.InnerText = string.Format("{0},{1},{2}", startPoint.X, startPoint.Y, startPoint.Z);
            nodeEndPoint.InnerText = string.Format("{0},{1},{2}", endPoint.X, endPoint.Y, startPoint.Z);
            nodeElement.AppendChild(nodeStartPoint);
            nodeElement.AppendChild(nodeEndPoint);
        }

        internal IEnumerable<MyBeamObj> GetOverhangBeams(IEnumerable<Level> targetLevels)
        {
            return (from obSection in _xElements.Element("悬挑梁集合").Elements("悬挑梁")
                    join targetLv in targetLevels
                    on obSection.Attribute("标高名称").Value equals targetLv.Name
                    select new MyBeamObj
                    {
                        Curve = Line.CreateBound(StringParseXYZ(obSection.Element("起点坐标").Value), StringParseXYZ(obSection.Element("终点坐标").Value)),
                        Level = targetLv
                    });
        }
        #endregion

        #region 垫板
        internal void WriteBottomPlateXml(XYZ pos, double baseHeight, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("垫板集合");
            var nodeElement = _xmlDoc.CreateElement("垫板");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[0].Name);
            nodeElement.InnerText = string.Format("{0},{1},{2}", pos.X, pos.Y, baseHeight);
            nodeCollection.AppendChild(nodeElement);
        }

        internal IEnumerable<XYZ> GetBottomPlates(Level bottomLevel)
        {
            return (from bpSection in _xElements.Element("垫板集合").Elements("垫板")
                    where bpSection.Attribute("标高名称").Value == bottomLevel.Name
                    select StringParseXYZ(bpSection.Value));
        }
        #endregion

        #region 斜杆
        internal void WriteSlantRodXml(Curve c, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("斜杆集合");// 斜杆集合节点
            XYZ pointS = c.GetEndPoint(0);
            XYZ pointE = c.GetEndPoint(1);
            XYZ startXYZ = pointS;// 斜杆段起点顶点
            XYZ endXYZ = pointS;// 斜杆段终点顶点
            int i = 1;// 项目标高集合索引值
            using (Line baseLine = Line.CreateBound(pointS, new XYZ(pointE.X, pointE.Y, pointS.Z)))
            {
                while (i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP <= pointS.Z)
                    i++;
                for (; i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP < pointE.Z - 500 / 304.8/*余500毫米不切割*/; i++)// 标高层分段斜杆 && 需要继续切割斜杆
                {
                    using (Transform upTrf = Transform.CreateTranslation(new XYZ(0, 0, (gm.LevelSet[i].Elevation - pointS.Z + Global.COLUMN_BEYOND_TOP))))
                    using (Line lineOffset = baseLine.CreateTransformed(upTrf) as Line)
                    {
                        IntersectionResultArray resultArray;
                        c.Intersect(lineOffset, out resultArray);
                        endXYZ = resultArray.get_Item(0).XYZPoint;
                        var nodeElement = _xmlDoc.CreateElement("斜杆段");// 斜杆标高名称子节点
                        nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 判断解决缺少首层标高
                        nodeCollection.AppendChild(nodeElement);
                        var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 斜杆起点坐标子节点
                        var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 斜杆终点坐标子节点
                        nodeStartPoint.InnerText = string.Format("{0},{1},{2}", startXYZ.X, startXYZ.Y, startXYZ.Z);
                        nodeEndPoint.InnerText = string.Format("{0},{1},{2}", endXYZ.X, endXYZ.Y, endXYZ.Z);
                        nodeElement.AppendChild(nodeStartPoint);
                        nodeElement.AppendChild(nodeEndPoint);
                        startXYZ = endXYZ;
                    }
                }
                if (endXYZ.Z < pointE.Z)// 未定义标高层分段斜杆 || 无需继续切割斜杆
                {
                    var nodeElement = _xmlDoc.CreateElement("斜杆段");// 斜杆标高名称子节点
                    nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 无需切割 : 未定义标高层分段斜杆
                    nodeCollection.AppendChild(nodeElement);
                    var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 斜杆起点坐标子节点
                    var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 斜杆终点坐标子节点
                    nodeStartPoint.InnerText = string.Format("{0},{1},{2}", startXYZ.X, startXYZ.Y, startXYZ.Z);
                    nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, pointE.Z);
                    nodeElement.AppendChild(nodeStartPoint);
                    nodeElement.AppendChild(nodeEndPoint);
                }
            }
        }

        internal List<MyPipeObj> GetSlantRodsByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyPipeObj> result = new List<MyPipeObj>();
            foreach (var item in (from srSection in _xElements.Element("斜杆集合").Elements("斜杆段")
                                  join targetLv in targetLevels
                                  on srSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPointString = srSection.Element("起点坐标").Value,
                                      EndPointString = srSection.Element("终点坐标").Value,
                                      BaseLevel = targetLv
                                  }))
                result.Add(new MyPipeObj { StartPoint = StringParseXYZ(item.StartPointString), EndPoint = StringParseXYZ(item.EndPointString), BaseLevel = item.BaseLevel });
            return result;
        }
        #endregion
    }
}