using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace ScaffoldTool
{
    internal class PKScaffoldXmlHelper : ScaffoldXmlHelperBase
    {
        private readonly static string PathExtension = @"盘扣式分层构件.xml";

        private PKScaffoldXmlHelper(Document doc)
        {
            _xmlPath = Path.ChangeExtension(doc.PathName, PathExtension);
        }

        public static PKScaffoldXmlHelper GetWritableXmlHelper(Document doc, bool overWrite = false)
        {
            PKScaffoldXmlHelper writableXml = new PKScaffoldXmlHelper(doc);
            if (overWrite)
                writableXml.CreateXml();
            else
                writableXml.LoadWritableXml();
            return writableXml;
        }

        public static PKScaffoldXmlHelper GetReadableXmlHelper(Document doc)
        {
            PKScaffoldXmlHelper readableXml = new PKScaffoldXmlHelper(doc);
            readableXml.LoadXml();
            return readableXml;
        }

        public override void CreateXml()
        {
            base.CreateXml();
            XmlNode nodeTop = _xmlDoc.SelectSingleNode("构件");
            XmlNode connectPlateCollection = _xmlDoc.CreateElement("连接盘集合");
            nodeTop.AppendChild(connectPlateCollection);
            connectPlateCollection.AppendChild(_xmlDoc.CreateElement("连接盘定位点集合"));
            connectPlateCollection.AppendChild(_xmlDoc.CreateElement("连接盘高度集合"));
            nodeTop.AppendChild(_xmlDoc.CreateElement("支座集合"));
        }

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

        #region 斜杆
        internal void WriteSlantRodXml(Curve c, double[] arrayOffset, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("斜杆集合");// 斜杆集合节点
            XYZ pointS = c.GetEndPoint(0);
            XYZ pointE = c.GetEndPoint(1);
            int i = 1;// 项目标高集合索引值
            var nodeElement = _xmlDoc.CreateElement("斜杆段");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 首层斜杆构件
            nodeCollection.AppendChild(nodeElement);
            var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 斜杆起点坐标子节点
            var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 斜杆终点坐标子节点
            nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, pointS.Z);
            nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, pointE.Z);
            nodeElement.AppendChild(nodeStartPoint);
            nodeElement.AppendChild(nodeEndPoint);
            for (int j = 0; j < arrayOffset.Length - 1; j++)
            {
                if (pointS.Z + arrayOffset[j] > gm.endElevation)
                    break;
                nodeElement = _xmlDoc.CreateElement("斜杆段");
                if (i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP < pointS.Z + arrayOffset[j])
                    i++;
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 斜杆起点坐标子节点
                nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 斜杆终点坐标子节点
                nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, pointS.Z + arrayOffset[j]);
                nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, pointE.Z + arrayOffset[j]);
                nodeElement.AppendChild(nodeStartPoint);
                nodeElement.AppendChild(nodeEndPoint);
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

        #region 连接盘
        internal void WriteConnectPlateXml_1(XYZ pos)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("连接盘集合").SelectSingleNode("连接盘定位点集合");
            var nodeElement = _xmlDoc.CreateElement("连接盘定位点");
            nodeElement.InnerText = string.Format("{0},{1},{2}", pos.X, pos.Y, 0);
            nodeCollection.AppendChild(nodeElement);
        }

        internal void WriteConnectPlateXml_2(double baseHeight, double topHeight, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("连接盘集合").SelectSingleNode("连接盘高度集合");
            double hight = baseHeight + Global.PIPE_BASE_OFFSET;
            int i = 1;// 项目标高集合索引值
            while (hight < topHeight)
            {
                var nodeElement = _xmlDoc.CreateElement("连接盘高度");
                if (i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP < hight)
                    i++;
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                var nodeOffset = _xmlDoc.CreateElement("偏移");
                nodeOffset.InnerText = (hight - gm.LevelSet[i - 1].Elevation).ToString();
                nodeElement.AppendChild(nodeOffset);
                hight += 500 / 304.8;
            }
        }

        internal List<MyPointObj> GetConnectPlatesByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyPointObj> result = new List<MyPointObj>();
            XElement[] cpLocs = _xElements.Element("连接盘集合").Element("连接盘定位点集合").Elements("连接盘定位点").ToArray();
            foreach (var item in (from cpHighSection in _xElements.Element("连接盘集合").Element("连接盘高度集合").Elements("连接盘高度")
                                  join targetLv in targetLevels
                                  on cpHighSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      Offset = double.Parse(cpHighSection.Element("偏移").Value),
                                      BaseLevel = targetLv
                                  }))
            {
                foreach (var loc in cpLocs)
                {
                    result.Add(new MyPointObj { Location = StringParseXYZ(loc.Value), Offset = item.Offset, BaseLevel = item.BaseLevel });
                }
            }
            return result;
        }
        #endregion

        #region 支座
        internal void WriteBearingXml(XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("支座集合");
            var nodeElement = _xmlDoc.CreateElement("支座");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[0].Name);
            nodeElement.InnerText = (gm.startElevation - gm.LevelSet[0].Elevation).ToString();
            nodeCollection.AppendChild(nodeElement);
        }

        internal List<MyPointObj> GetBearingsXml(Level bottomLevel)
        {
            List<MyPointObj> result = new List<MyPointObj>();
            XElement[] bSections = (from bSection in _xElements.Element("支座集合").Elements("支座")
                                    where bSection.Attribute("标高名称").Value == bottomLevel.Name
                                    select bSection).ToArray();
            if (bSections.Length > 0)
            {
                XElement[] cpLocs = _xElements.Element("连接盘集合").Element("连接盘定位点集合").Elements("连接盘定位点").ToArray();
                foreach (var loc in cpLocs)
                {
                    result.Add(new MyPointObj { Location = StringParseXYZ(loc.Value), Offset = double.Parse(bSections[0].Value), BaseLevel = bottomLevel });
                }
            }
            return result;
        }
        #endregion
    }

    internal class MyPointObj
    {
        public Level BaseLevel { get; internal set; }
        public XYZ Location { get; internal set; }
        public double Offset { get; internal set; }
    }
}
