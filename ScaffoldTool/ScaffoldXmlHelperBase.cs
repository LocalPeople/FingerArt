using Autodesk.Revit.DB;
using ScaffoldTool.ScaffoldComponent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ScaffoldTool
{
    abstract class ScaffoldXmlHelperBase
    {
        protected string _xmlPath;
        protected XmlDocument _xmlDoc;
        protected XElement _xElements;

        public bool IsOnGroundScaffoldXml
        {
            get
            {
                return _xElements.Element("垫板集合").Elements("垫板").Count() > 0;
            }
        }

        public void LoadXml()
        {
            if (_xElements != null)
                return;
            _xElements = XElement.Load(_xmlPath);
        }

        public virtual void CreateXml()
        {
            if (_xmlDoc != null)
                return;
            _xmlDoc = new XmlDocument();
            XmlNode nodeTop = _xmlDoc.CreateElement("构件");// 顶部节点
            _xmlDoc.AppendChild(nodeTop);
            XmlNode undoCollection = _xmlDoc.CreateElement("回退集合");
            nodeTop.AppendChild(undoCollection);
            XmlNode redoneCollection = _xmlDoc.CreateElement("重做集合");
            nodeTop.AppendChild(redoneCollection);
            XmlNode levelCollection = _xmlDoc.CreateElement("标高集合");
            nodeTop.AppendChild(levelCollection);
            XmlNode columnCollection = _xmlDoc.CreateElement("立杆集合");
            nodeTop.AppendChild(columnCollection);
            XmlNode slantRodCollection = _xmlDoc.CreateElement("斜杆集合");
            nodeTop.AppendChild(slantRodCollection);
            XmlNode rowCollection = _xmlDoc.CreateElement("横杆集合");
            nodeTop.AppendChild(rowCollection);
            XmlNode boardCollection1 = _xmlDoc.CreateElement("脚手板集合");
            nodeTop.AppendChild(boardCollection1);
            XmlNode boardCollection2 = _xmlDoc.CreateElement("挡脚板集合");
            nodeTop.AppendChild(boardCollection2);
            XmlNode boardCollection3 = _xmlDoc.CreateElement("安全网集合");
            nodeTop.AppendChild(boardCollection3);
            XmlNode fixtureCollection = _xmlDoc.CreateElement("连墙件集合");
            nodeTop.AppendChild(fixtureCollection);
        }

        public void LoadWritableXml()
        {
            if (_xmlDoc != null)
                return;
            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(_xmlPath);
        }

        public void EditQuit()
        {
            _xmlDoc.Save(_xmlPath);
            _xmlDoc = null;
            _xmlPath = null;
        }

        #region 回退及重做
        public static void Undo(Document doc, string extension)
        {
            string xmlPath = Path.ChangeExtension(doc.PathName, extension);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);
            XmlNode nodeCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("标高集合");
            XmlNode undoCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("回退集合");
            XmlNode redoneCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("重做集合");

            if (undoCollection.LastChild.Name == "创建")
            {
                string[] toDelete = undoCollection.LastChild.InnerText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toDelete.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "false";
                }
            }
            else if (undoCollection.LastChild.Name == "删除")
            {
                string[] toBuild = undoCollection.LastChild.InnerText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toBuild.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "true";
                }
            }

            redoneCollection.AppendChild(undoCollection.LastChild.Clone());
            undoCollection.RemoveChild(undoCollection.LastChild);
            xmlDoc.Save(xmlPath);
        }

        public static void Redone(Document doc, string extension)
        {
            string xmlPath = Path.ChangeExtension(doc.PathName, extension);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);
            XmlNode nodeCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("标高集合");
            XmlNode undoCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("回退集合");
            XmlNode redoneCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("重做集合");

            if (redoneCollection.LastChild.Name == "创建")
            {
                string[] toBuild = redoneCollection.LastChild.InnerText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toBuild.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "true";
                }
            }
            else if (redoneCollection.LastChild.Name == "删除")
            {
                string[] toDelete = redoneCollection.LastChild.InnerText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toDelete.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "false";
                }
            }

            undoCollection.AppendChild(redoneCollection.LastChild.Clone());
            redoneCollection.RemoveChild(redoneCollection.LastChild);
            xmlDoc.Save(xmlPath);
        }

        public static void Clear(Document doc, string extension)
        {
            string xmlPath = Path.ChangeExtension(doc.PathName, extension);
            if (File.Exists(xmlPath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);
                xmlDoc.SelectSingleNode("构件").SelectSingleNode("回退集合").RemoveAll();
                xmlDoc.SelectSingleNode("构件").SelectSingleNode("重做集合").RemoveAll();
                xmlDoc.Save(xmlPath);
            }
        }
        #endregion

        #region 分层模型记录
        public void WriteLevelXml(XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("标高集合");
            foreach (var item in gm.LevelSet.Reverse())
            {
                var nodeElement = _xmlDoc.CreateElement("标高");
                nodeElement.SetAttribute("标高名称", item.Name);
                nodeElement.SetAttribute("是否生成脚手架", "false");
                nodeCollection.AppendChild(nodeElement);
            }
        }

        protected static void GetBuildInfo(Document doc, out List<string> alreadyBuilt, out List<string> notYetBuilt, string extension)
        {
            alreadyBuilt = new List<string>();
            notYetBuilt = new List<string>();
            XElement xElements = XElement.Load(Path.ChangeExtension(doc.PathName, extension));
            foreach (var item in xElements.Element("标高集合").Elements())
            {
                if (item.Attribute("是否生成脚手架").Value == "false")
                    notYetBuilt.Add(item.Attribute("标高名称").Value);
                else
                    alreadyBuilt.Add(item.Attribute("标高名称").Value);
            }
        }

        protected static void UpdateBuildInfo(Document doc, IEnumerable<string> toBuild, IEnumerable<string> toDelete, string extension)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.ChangeExtension(doc.PathName, extension);
            xmlDoc.Load(xmlPath);
            XmlNode nodeCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("标高集合");
            XmlNode undoCollection = xmlDoc.SelectSingleNode("构件").SelectSingleNode("回退集合");
            StringBuilder sb = new StringBuilder();
            if (toBuild.Count() > 0)
            {
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toBuild.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "true";
                }
                XmlNode buildElement = xmlDoc.CreateElement("创建");
                undoCollection.AppendChild(buildElement);
                foreach (var s in toBuild)
                    sb.Append(s + " ");
                buildElement.InnerText = sb.ToString().TrimEnd(' ');
            }
            else if (toDelete.Count() > 0)
            {
                foreach (XmlNode node in nodeCollection.ChildNodes)
                {
                    if (toDelete.Contains(node.Attributes["标高名称"].Value))
                        node.Attributes["是否生成脚手架"].Value = "false";
                }
                XmlNode deleteElement = xmlDoc.CreateElement("删除");
                undoCollection.AppendChild(deleteElement);
                foreach (var s in toDelete)
                    sb.Append(s + " ");
                deleteElement.InnerText = sb.ToString().TrimEnd(' ');
            }
            xmlDoc.Save(xmlPath);
        }
        #endregion

        #region 立杆通用
        internal void WriteColumnXml(XYZ pos, double baseHeight, double topHeight, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("立杆集合");// 立杆集合节点
            double startHeight = baseHeight;
            double endHeight = baseHeight;
            int i = 1;// 项目标高集合索引值
            for (; i < gm.LevelSet.Length && endHeight < topHeight; i++)// 标高层分段立杆
            {
                endHeight = gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP;
                if (endHeight > topHeight)
                    return;
                var nodeElement = _xmlDoc.CreateElement("立杆段");// 立杆标高名称子节点
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 判断解决缺少首层标高
                nodeCollection.AppendChild(nodeElement);
                var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 立杆起点坐标子节点
                var nodeOffset = _xmlDoc.CreateElement("偏移");
                var nodeHeight = _xmlDoc.CreateElement("高度");
                nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pos.X, pos.Y, 0);
                nodeOffset.InnerText = (startHeight - gm.LevelSet[i - 1].Elevation).ToString();
                nodeHeight.InnerText = (endHeight - startHeight).ToString();
                nodeElement.AppendChild(nodeStartPoint);
                nodeElement.AppendChild(nodeOffset);
                nodeElement.AppendChild(nodeHeight);
                startHeight = endHeight;
            }
            if (endHeight < topHeight)// 未定义标高层分段立杆
            {
                var nodeElement = _xmlDoc.CreateElement("立杆段");// 立杆标高名称子节点
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 立杆起点坐标子节点
                var nodeOffset = _xmlDoc.CreateElement("偏移");
                var nodeHeight = _xmlDoc.CreateElement("高度");
                nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pos.X, pos.Y, 0);
                nodeOffset.InnerText = (startHeight - gm.LevelSet[i - 1].Elevation).ToString();
                nodeHeight.InnerText = (topHeight - startHeight).ToString();
                nodeElement.AppendChild(nodeStartPoint);
                nodeElement.AppendChild(nodeOffset);
                nodeElement.AppendChild(nodeHeight);
            }
        }

        internal List<MyPipeObj> GetColumnsByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyPipeObj> result = new List<MyPipeObj>();
            foreach (var item in (from colSection in _xElements.Element("立杆集合").Elements("立杆段")
                                  join targetLv in targetLevels
                                  on colSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPointString = colSection.Element("起点坐标").Value,
                                      Offset = colSection.Element("偏移").Value,
                                      Height = colSection.Element("高度").Value,
                                      BaseLevel = targetLv
                                  }))
                result.Add(new MyPipeObj { StartPoint = StringParseXYZ(item.StartPointString), BaseLevel = item.BaseLevel, Offset = double.Parse(item.Offset), Height = double.Parse(item.Height) });
            return result;
        }
        #endregion

        #region 横杆通用
        internal void WriteRowXml(Curve c, double[] arrayOffset, XC_GenericModel gm, double topAdd)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("横杆集合");// 横杆集合节点
            XYZ pointS = c.GetEndPoint(0);
            XYZ pointE = c.GetEndPoint(1);
            int i = 1;// 项目标高集合索引值
            var nodeElement = _xmlDoc.CreateElement("横杆段");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 首层横杆构件
            nodeCollection.AppendChild(nodeElement);
            var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 横杆起点坐标子节点
            var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 横杆终点坐标子节点
            nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, 0);
            nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, 0);
            nodeElement.AppendChild(nodeStartPoint);
            nodeElement.AppendChild(nodeEndPoint);
            var nodeOffset = _xmlDoc.CreateElement("偏移");
            nodeOffset.InnerText = (pointS.Z - gm.LevelSet[i - 1].Elevation).ToString();
            nodeElement.AppendChild(nodeOffset);
            foreach (double offset in arrayOffset)
            {
                if (pointS.Z + offset > gm.endElevation + topAdd)
                    break;
                nodeElement = _xmlDoc.CreateElement("横杆段");
                if (i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP < pointS.Z + offset)
                    i++;
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                nodeStartPoint = _xmlDoc.CreateElement("起点坐标");// 横杆起点坐标子节点
                nodeEndPoint = _xmlDoc.CreateElement("终点坐标");// 横杆终点坐标子节点
                nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, 0);
                nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, 0);
                nodeElement.AppendChild(nodeStartPoint);
                nodeElement.AppendChild(nodeEndPoint);
                nodeOffset = _xmlDoc.CreateElement("偏移");
                nodeOffset.InnerText = (pointS.Z + offset - gm.LevelSet[i - 1].Elevation).ToString();
                nodeElement.AppendChild(nodeOffset);
            }
        }

        internal List<MyPipeObj> GetRowsByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyPipeObj> result = new List<MyPipeObj>();
            foreach (var item in (from rowSection in _xElements.Element("横杆集合").Elements("横杆段")
                                  join targetLv in targetLevels
                                  on rowSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPointString = rowSection.Element("起点坐标").Value,
                                      EndPointString = rowSection.Element("终点坐标").Value,
                                      BaseLevel = targetLv,
                                      Offset = double.Parse(rowSection.Element("偏移").Value)
                                  }))
                result.Add(new MyPipeObj { StartPoint = StringParseXYZ(item.StartPointString), EndPoint = StringParseXYZ(item.EndPointString), BaseLevel = item.BaseLevel, Offset = item.Offset });
            return result;
        }
        #endregion

        #region 安全网
        internal void WriteNetXml(Curve curve, List<double> bjHeightArray, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("安全网集合");
            XYZ pointS = curve.GetEndPoint(0);
            XYZ pointE = curve.GetEndPoint(1);
            int i = 1;// 项目标高集合索引值
            double netHeight = Global.BJ - 3 * Global.D;// 安全网固定高度
            foreach (var bjHeight in bjHeightArray)
            {
                if (i < gm.LevelSet.Length && bjHeight + netHeight > gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP)
                    i++;
                double netOffset = bjHeight - gm.LevelSet[i - 1].Elevation;
                var nodeElement = _xmlDoc.CreateElement("安全网");
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                var nodeStartPonit = _xmlDoc.CreateElement("起点坐标");
                var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");
                nodeStartPonit.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, 0);
                nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, 0);
                nodeElement.AppendChild(nodeStartPonit);
                nodeElement.AppendChild(nodeEndPoint);
                var nodeHeight = _xmlDoc.CreateElement("高度");
                var nodeOffset = _xmlDoc.CreateElement("偏移");
                nodeHeight.InnerText = netHeight.ToString();
                nodeOffset.InnerText = netOffset.ToString();
                nodeElement.AppendChild(nodeHeight);
                nodeElement.AppendChild(nodeOffset);
            }
        }

        internal List<MyNetObj> GetNetsByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyNetObj> result = new List<MyNetObj>();
            foreach (var item in (from netSection in _xElements.Element("安全网集合").Elements("安全网")
                                  join targetLv in targetLevels
                                  on netSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPointString = netSection.Element("起点坐标").Value,
                                      EndPointString = netSection.Element("终点坐标").Value,
                                      BaseLevel = targetLv,
                                      Height = netSection.Element("高度").Value,
                                      Offset = netSection.Element("偏移").Value
                                  }))
                result.Add(new MyNetObj { Curve = Line.CreateBound(StringParseXYZ(item.StartPointString), StringParseXYZ(item.EndPointString)), BaseLevel = item.BaseLevel, Height = double.Parse(item.Height), Offset = double.Parse(item.Offset) });
            return result;
        }
        #endregion

        #region 挡脚板
        internal void WriteBorderXml(Curve c, double[] arrayOffset, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("挡脚板集合");
            XYZ pointS = c.GetEndPoint(0);
            XYZ pointE = c.GetEndPoint(1);
            int i = 1;
            double borderOffset = pointS.Z - gm.LevelSet[i - 1].Elevation;
            var nodeElement = _xmlDoc.CreateElement("挡脚板");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
            nodeCollection.AppendChild(nodeElement);
            var nodeStartPonit = _xmlDoc.CreateElement("起点坐标");
            var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");
            nodeStartPonit.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, 0);
            nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, 0);
            nodeElement.AppendChild(nodeStartPonit);
            nodeElement.AppendChild(nodeEndPoint);
            var nodeOffset = _xmlDoc.CreateElement("偏移");
            nodeOffset.InnerText = borderOffset.ToString();
            nodeElement.AppendChild(nodeOffset);
            foreach (var offset in arrayOffset)
            {
                while (i < gm.LevelSet.Length && pointS.Z + offset > gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP)
                    i++;
                borderOffset = pointS.Z + offset - gm.LevelSet[i - 1].Elevation;
                nodeElement = _xmlDoc.CreateElement("挡脚板");
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                nodeStartPonit = _xmlDoc.CreateElement("起点坐标");
                nodeEndPoint = _xmlDoc.CreateElement("终点坐标");
                nodeStartPonit.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, 0);
                nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, 0);
                nodeElement.AppendChild(nodeStartPonit);
                nodeElement.AppendChild(nodeEndPoint);
                nodeOffset = _xmlDoc.CreateElement("偏移");
                nodeOffset.InnerText = borderOffset.ToString();
                nodeElement.AppendChild(nodeOffset);
            }
        }

        internal List<MyBorderObj> GetBordersByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyBorderObj> result = new List<MyBorderObj>();
            foreach (var item in (from bdSection in _xElements.Element("挡脚板集合").Elements("挡脚板")
                                  join targetLv in targetLevels
                                  on bdSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPointString = bdSection.Element("起点坐标").Value,
                                      EndPointString = bdSection.Element("终点坐标").Value,
                                      BaseLevel = targetLv,
                                      Offset = bdSection.Element("偏移").Value
                                  }))
                result.Add(new MyBorderObj { Curve = Line.CreateBound(StringParseXYZ(item.StartPointString), StringParseXYZ(item.EndPointString)), BaseLevel = item.BaseLevel, Offset = double.Parse(item.Offset) });
            return result;
        }
        #endregion

        #region 脚手板
        internal void WriteFootBoardXml(CurveArray curveArray, double[] arrayOffset, XC_GenericModel gm)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("脚手板集合");
            XYZ[] coordinates = new XYZ[curveArray.Size];// 顶点集合
            int i = 0;
            for (; i < curveArray.Size; i++)
                coordinates[i] = curveArray.get_Item(i).GetEndPoint(0);
            i = 1;// 项目标高集合索引值
            var nodeElement = _xmlDoc.CreateElement("脚手板");
            nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);// 首层构件
            nodeCollection.AppendChild(nodeElement);
            foreach (var point in coordinates)
            {
                var nodeCoordinate = _xmlDoc.CreateElement("顶点");
                nodeCoordinate.InnerText = string.Format("{0},{1},{2}", point.X, point.Y, point.Z);
                nodeElement.AppendChild(nodeCoordinate);
            }
            foreach (double offset in arrayOffset)
            {
                nodeElement = _xmlDoc.CreateElement("脚手板");
                while (i < gm.LevelSet.Length && gm.LevelSet[i].Elevation + Global.COLUMN_BEYOND_TOP < coordinates[0].Z + offset)
                    i++;
                nodeElement.SetAttribute("标高名称", gm.LevelSet[i - 1].Name);
                nodeCollection.AppendChild(nodeElement);
                foreach (var point in coordinates)
                {
                    var nodeCoordinate = _xmlDoc.CreateElement("顶点");
                    nodeCoordinate.InnerText = string.Format("{0},{1},{2}", point.X, point.Y, point.Z + offset);
                    nodeElement.AppendChild(nodeCoordinate);
                }
            }
        }

        internal List<MyFloorObj> GetFootBoardsByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyFloorObj> result = new List<MyFloorObj>();
            foreach (var item in (from fbSection in _xElements.Element("脚手板集合").Elements("脚手板")
                                  join targetLv in targetLevels
                                  on fbSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      FbSection = fbSection,
                                      BaseLevel = targetLv
                                  }))
            {
                CurveArray curveArray = new CurveArray();
                XElement[] nodeCoordinates = item.FbSection.Elements("顶点").ToArray();
                for (int i = 0; i < nodeCoordinates.Length; i++)
                    curveArray.Append(Line.CreateBound(StringParseXYZ(nodeCoordinates[i].Value), StringParseXYZ(nodeCoordinates[(i + 1) % nodeCoordinates.Length].Value)));
                result.Add(new MyFloorObj { CurveArray = curveArray, BaseLevel = item.BaseLevel });
            }
            return result;
        }
        #endregion

        #region 连墙件
        internal void WriteFixtureXml(List<ScaffoldFixture> sfList)
        {
            XmlNode nodeCollection = _xmlDoc.SelectSingleNode("构件").SelectSingleNode("连墙件集合");
            foreach (var sf in sfList)
                foreach (Curve c in sf.Curves)
                {
                    var nodeElement = _xmlDoc.CreateElement("连墙件");
                    nodeElement.SetAttribute("标高名称", sf.Level.Name);
                    nodeCollection.AppendChild(nodeElement);
                    XYZ pointS = c.GetEndPoint(0);
                    XYZ pointE = c.GetEndPoint(1);
                    var nodeStartPoint = _xmlDoc.CreateElement("起点坐标");
                    var nodeEndPoint = _xmlDoc.CreateElement("终点坐标");
                    var nodeOffset = _xmlDoc.CreateElement("偏移");
                    nodeStartPoint.InnerText = string.Format("{0},{1},{2}", pointS.X, pointS.Y, pointS.Z);
                    nodeEndPoint.InnerText = string.Format("{0},{1},{2}", pointE.X, pointE.Y, pointE.Z);
                    nodeOffset.InnerText = (pointS.Z - sf.Level.Elevation).ToString();
                    nodeElement.AppendChild(nodeStartPoint);
                    nodeElement.AppendChild(nodeEndPoint);
                    nodeElement.AppendChild(nodeOffset);
                }
        }

        internal List<MyPipeObj> GetFixturesByLevels(IEnumerable<Level> targetLevels)
        {
            List<MyPipeObj> result = new List<MyPipeObj>();
            foreach (var item in (from ftSection in _xElements.Element("连墙件集合").Elements("连墙件")
                                  join targetLv in targetLevels
                                  on ftSection.Attribute("标高名称").Value equals targetLv.Name
                                  select new
                                  {
                                      StartPoint = StringParseXYZ(ftSection.Element("起点坐标").Value),
                                      EndPoint = StringParseXYZ(ftSection.Element("终点坐标").Value),
                                      Offset = ftSection.Element("偏移").Value,
                                      BaseLevel = targetLv,
                                  }))
            {
                if (Math.Abs(item.StartPoint.Z - item.EndPoint.Z) < 0.1)
                    result.Add(new MyPipeObj { StartPoint = item.StartPoint, EndPoint = item.EndPoint, BaseLevel = item.BaseLevel, Offset = double.Parse(item.Offset), IsHorizontal = true });
                else
                    result.Add(new MyPipeObj { StartPoint = item.StartPoint, Height = item.EndPoint.Z - item.StartPoint.Z, BaseLevel = item.BaseLevel, Offset = double.Parse(item.Offset), IsHorizontal = false });
            }
            return result;
        }
        #endregion

        protected XYZ StringParseXYZ(string value)
        {
            string[] vertex = value.Split(',');
            return new XYZ(double.Parse(vertex[0]), double.Parse(vertex[1]), double.Parse(vertex[2]));
        }
    }

    #region 构件数据类
    internal class MyBorderObj
    {
        public Level BaseLevel { get; internal set; }
        public Line Curve { get; internal set; }
        public double Offset { get; internal set; }
    }

    internal class MyNetObj
    {
        public Level BaseLevel { get; internal set; }
        public Line Curve { get; internal set; }
        public double Height { get; internal set; }
        public double Offset { get; internal set; }
    }

    internal class MyFloorObj
    {
        public Level BaseLevel { get; internal set; }
        public CurveArray CurveArray { get; internal set; }
    }

    internal class MyBeamObj
    {
        public Line Curve { get; internal set; }
        public Level Level { get; internal set; }
    }

    internal class MyPipeObj
    {
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public Level BaseLevel { get; set; }
        public double Offset { get; internal set; }
        public double Height { get; internal set; }
        public bool IsHorizontal { get; internal set; }
    }
    #endregion
}
