using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace ScaffoldTool
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RequireScaffoldParam : IExternalCommand
    {
        private XcWpfControlLib.ProgressArgs progressBar;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // 获取外轮廓及标高信息
            Reference reference;
            if (PickObject(uiDoc, out reference))
                return Result.Cancelled;

            progressBar = XcWpfControlLib.ProgressBarWindow.ProgressShow();

            XC_GenericModel gm = new XC_GenericModel(doc.GetElement(reference) as FamilyInstance);

            // 计算脚手架参数
            XC_PKScaffold scaffold = new XC_PKScaffold(doc, gm);
            scaffold.ProgressStart += Scaffold_ProgressStart;
            scaffold.ProgressReport += Scaffold_ProgressReport;

            // 盘扣式脚手架测试代码
            scaffold.CalculateLines();
            scaffold.CalculateCorners();
            scaffold.CalculateConnects();
            scaffold.CalculateSlantRods();
            scaffold.CalculateFixtures();
            scaffold.CalculateBoards(100 / 304.8);
            Level baseLevel = scaffold.HostGenericModel.LevelSet[0];
            double baseHeight = scaffold.HostGenericModel.LevelSet[0].Elevation + scaffold.HostGenericModel.LevelSet.Base2BaseOffset + 100 / 304.8;// 填入底部开始创建偏移量
            double topHeight = scaffold.HostGenericModel.LevelSet[0].Elevation + scaffold.HostGenericModel.LevelSet.Top2BaseOffset + Global.COLUMN_BEYOND_TOP;
            PKScaffoldXmlHelper writableXml = PKScaffoldXmlHelper.GetWritableXmlHelper(doc, true);
            // 立杆 && 连接盘
            foreach (var pos in scaffold.GetScaffoldAllColumns())
            {
                writableXml.WriteColumnXml(pos, baseHeight, topHeight, gm);
                writableXml.WriteConnectPlateXml_1(pos);
            }
            writableXml.WriteConnectPlateXml_2(baseHeight, topHeight, gm);
            writableXml.WriteBearingXml(gm);

            // 横杆
            Transform topToRowStart = Transform.CreateTranslation(new XYZ(0, 0, baseHeight + Global.PIPE_BASE_OFFSET - scaffold.HostGenericModel.endElevation));
            double[] offsetArray = XC_ScaffoldCreation.GetRowCopyOffsetList(scaffold.HostGenericModel, 100 / 304.8).ToArray();
            foreach (var line in scaffold.GetScaffoldAllHorizontalRods())
                using (Curve c = line.CreateTransformed(topToRowStart))
                {
                    writableXml.WriteRowXml(c, offsetArray, gm, Global.COLUMN_BEYOND_TOP);
                }

            // 斜杆
            foreach (var line in scaffold.GetScaffoldAllSlantRods())
                using (Curve c = line.CreateTransformed(topToRowStart))
                {
                    writableXml.WriteSlantRodXml(c, offsetArray, gm);
                }

            // 板类（脚手板、挡脚板、安全网）
            double[] arrayOffset2 = XC_ScaffoldCreation.GetBoardCopyOffsetList(scaffold.HostGenericModel).ToArray();
            List<double> arrayOffset3 = XC_ScaffoldCreation.GetAQWHeightList(scaffold.HostGenericModel, 100 / 304.8);// 安全网分层高度集合
            foreach (CurveArray[] cArray in scaffold.GetScaffoldBoards())
            {
                using (cArray[0])
                    writableXml.WriteFootBoardXml(cArray[0], arrayOffset2, scaffold.HostGenericModel);
                using (cArray[1])
                    writableXml.WriteBorderXml(cArray[1].get_Item(0), arrayOffset2, scaffold.HostGenericModel);
                using (cArray[2])
                    writableXml.WriteNetXml(cArray[2].get_Item(0), arrayOffset3, scaffold.HostGenericModel);
            }

            // 连墙件
            writableXml.WriteFixtureXml(scaffold.GetScaffoldFixtures());

            // 标高记录
            writableXml.WriteLevelXml(scaffold.HostGenericModel);
            writableXml.EditQuit();

            List<string> alreadyBuilt, notYetBuilt;
            PKScaffoldXmlHelper.GetBuildInfo(doc, out alreadyBuilt, out notYetBuilt);
            Level[] modelingLevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .Where(elem => notYetBuilt.Contains(elem.Name)).OrderBy(elem => elem.Elevation).ToArray();

            // 生成脚手架
            using (Transaction trans = new Transaction(doc, "ScaffoldTool"))
            {
                trans.Start();
                PKScaffoldXmlHelper readableXml = PKScaffoldXmlHelper.GetReadableXmlHelper(doc);
                FamilySymbol columnType = XC_ScaffoldCreation.GetCustomType(doc, "星层_扣件式立杆");
                if (!columnType.IsActive)
                    columnType.Activate();
                List<MyPipeObj> list1 = readableXml.GetColumnsByLevels(modelingLevel);
                progressBar.Maximum = list1.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成立杆……";
                foreach (var item in list1)// 立杆
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.StartPoint, columnType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    instance.LookupParameter("高度").Set(item.Height);
                    progressBar.Change();
                }
                list1.Clear();

                FamilySymbol rowType = XC_ScaffoldCreation.GetCustomType(doc, "星层_盘扣式横杆");
                if (!rowType.IsActive)
                    rowType.Activate();
                List<MyPipeObj> list2 = readableXml.GetRowsByLevels(modelingLevel);
                progressBar.Maximum = list2.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成横杆……";
                foreach (var item in list2)// 横杆
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(Line.CreateBound(item.StartPoint, item.EndPoint), rowType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    progressBar.Change();
                }
                list2.Clear();

                FamilySymbol cpType = XC_ScaffoldCreation.GetCustomType(doc, "星层_盘扣式连接盘");
                if (!cpType.IsActive)
                    cpType.Activate();
                List<MyPointObj> list3 = readableXml.GetConnectPlatesByLevels(modelingLevel);
                progressBar.Maximum = list3.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成连接盘……";
                foreach (var item in list3)
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.Location, cpType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    progressBar.Change();
                }
                list3.Clear();

                FamilySymbol bearingType = XC_ScaffoldCreation.GetCustomType(doc, "星层_盘扣式底座");
                if (!bearingType.IsActive)
                    bearingType.Activate();
                List<MyPointObj> list4 = readableXml.GetBearingsXml(modelingLevel.First());
                progressBar.Maximum = list4.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成底座……";
                foreach (var item in list4)
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.Location, bearingType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    progressBar.Change();
                }
                list4.Clear();

                FloorType floorType1 = XC_ScaffoldCreation.GetFloorType(doc, "脚手板", Global.SCAFFOLD_HORIZONTAL_FLOOR_THICK);
                List<MyFloorObj> list5 = readableXml.GetFootBoardsByLevels(modelingLevel);
                progressBar.Maximum = list5.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成脚手板……";
                foreach (var item in list5)// 脚手板
                {
                    doc.Create.NewFloor(item.CurveArray, floorType1, item.BaseLevel, false, XYZ.BasisZ);
                    progressBar.Change();
                }
                list5.Clear();

                FamilySymbol floorType2 = XC_ScaffoldCreation.GetCustomType(doc, "星层_挡脚板");
                if (!floorType2.IsActive)
                    floorType2.Activate();
                List<MyBorderObj> list6 = readableXml.GetBordersByLevels(modelingLevel);
                progressBar.Maximum = list6.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成挡脚板……";
                foreach (var item in list6)// 挡脚板
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.Curve, floorType2, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    progressBar.Change();
                }
                list6.Clear();

                FamilySymbol wallType3 = XC_ScaffoldCreation.GetCustomType(doc, "星层_脚手架安全网");
                if (!wallType3.IsActive)
                    wallType3.Activate();
                List<MyNetObj> list7 = readableXml.GetNetsByLevels(modelingLevel);
                progressBar.Maximum = list7.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成安全网……";
                foreach (var item in list7)// 安全网
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.Curve, wallType3, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("高度").Set(item.Height);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    progressBar.Change();
                }
                list7.Clear();

                FamilySymbol fixtureRowType = XC_ScaffoldCreation.GetCustomType(doc, "星层_连墙件横杆");
                if (!fixtureRowType.IsActive)
                    fixtureRowType.Activate();
                FamilySymbol fixtureColumnType = XC_ScaffoldCreation.GetCustomType(doc, "星层_连墙件立杆");
                if (!fixtureColumnType.IsActive)
                    fixtureColumnType.Activate();
                List<MyPipeObj> list8 = readableXml.GetFixturesByLevels(modelingLevel);
                progressBar.Maximum = list8.Count;
                progressBar.Value = 0;
                progressBar.Tip = "正在生成连墙件……";
                foreach (var item in list8)// 连墙件
                {
                    if (item.IsHorizontal)
                    {
                        FamilyInstance instance = doc.Create.NewFamilyInstance(Line.CreateBound(item.StartPoint, item.EndPoint), fixtureRowType, item.BaseLevel, StructuralType.NonStructural);
                        instance.LookupParameter("偏移量").Set(item.Offset);
                    }
                    else
                    {
                        FamilyInstance instance = doc.Create.NewFamilyInstance(item.StartPoint, fixtureColumnType, item.BaseLevel, StructuralType.NonStructural);
                        instance.LookupParameter("偏移量").Set(item.Offset);
                        instance.LookupParameter("高度").Set(item.Height);
                    }
                    progressBar.Change();
                }
                list8.Clear();
                progressBar.Text = "正在等待模型重生成，请稍等……";
                progressBar.IsIndeterminate = true;
                trans.Commit();
            }

            progressBar.Close();
            if (progressBar.IsCancel)
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }

        private void Scaffold_ProgressReport(object sender, ScaffoldManager.ProgressReportEventArgs e)
        {
            progressBar.Change();
        }

        private void Scaffold_ProgressStart(object sender, ScaffoldManager.ProgressStartEventArgs e)
        {
            progressBar.Value = 0;
            progressBar.Maximum = e.ProgressMaximum;
            progressBar.Tip = e.CurrentProgress;
        }

        private bool PickObject(UIDocument uiDoc, out Reference reference)
        {
            try
            {
                reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new GenericModelSelectionFilter(), "请选取常规內建模型");
            }
            catch
            {
                reference = null;
            }
            return reference == null;
        }
    }
}
