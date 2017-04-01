using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using XcWpfControlLib.WpfScaffoldControlLib;

namespace ScaffoldTool
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SettingStartUp : IExternalCommand
    {
        private XcWpfControlLib.ProgressArgs progressBar;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            if (string.IsNullOrWhiteSpace(doc.PathName))
            {
                TaskDialog.Show("Revit", "请保存项目后再进行操作");
                return Result.Cancelled;
            }

            // 获取外轮廓及标高信息
            Reference reference;
            if (PickObject(uiDoc, out reference))
                return Result.Cancelled;
            XC_GenericModel gm = new XC_GenericModel(doc.GetElement(reference) as FamilyInstance);

            #region 界面
            ScaffoldWindow1 window = new ScaffoldWindow1(gm.LevelSet.Height, doc.PathName);
            if (window.ShowDialog() == false)
                return Result.Cancelled;

            Global.LGZJ = double.Parse(window.StructuralProperties["LGZJ"]) / 0.3048;// m to inch
            Global.LGHJ = double.Parse(window.StructuralProperties["LGHJ"]) / 0.3048;// m to inch
            Global.BJ = double.Parse(window.StructuralProperties["BJ"]) / 0.3048;// m to inch
            Global.LQJKS = int.Parse(window.StructuralProperties["LQJKS"]);
            Global.NJJQ = double.Parse(window.StructuralProperties["NJJQ"]) / 304.8;// mm to inch
            Global.BOTTOM_PLATE_LENGTH = Math.Sqrt(double.Parse(window.StructuralProperties["JCMJ"])) / 0.3048;// m to inch
            bool isBuildOnGrond = window.IsBuildOnGround;

            if (window != null)
                window.Close();
            #endregion

            progressBar = XcWpfControlLib.ProgressBarWindow.ProgressShow();

            #region 后台处理
            // 生成脚手架构件模型信息本地XML文件
            if (isBuildOnGrond)
            {
                XC_KJScaffold scaffold = new XC_KJScaffold(doc, gm);
                scaffold.ProgressStart += Scaffold_ProgressStart;// 订阅进度条事件
                scaffold.ProgressReport += Scaffold_ProgressReport;
                try
                {
                    XC_ScaffoldCreation.KJSaveOnGroundXml(doc, scaffold, true);
                }
                catch (Exception e)
                {
                    if (e.Message == "用户取消")
                    {
                        progressBar = null;
                        return Result.Cancelled;
                    }
                    throw;
                }
            }
            else
            {
                // 悬挑更改
                List<XC_GenericModel> gms = new List<XC_GenericModel>(gm.Devide(doc, Global.OVERHANG_MAX_DISTANCE));
                try
                {
                    XC_ScaffoldCreation.KJSaveOverhangXml(doc, new XC_KJScaffold(doc, gms[gms.Count - 1]), true, true);
                    for (int i = gms.Count - 2; i >= 0; i--)
                        XC_ScaffoldCreation.KJSaveOverhangXml(doc, new XC_KJScaffold(doc, gms[i]), false, false);
                    XC_KJScaffold scaffold = new XC_KJScaffold(doc, gm);
                    scaffold.ProgressStart += Scaffold_ProgressStart;// 订阅进度条事件
                    scaffold.ProgressReport += Scaffold_ProgressReport;
                    XC_ScaffoldCreation.KJSaveOverhangXmlExtend(doc, scaffold);
                }
                catch (Exception e)
                {
                    if (e.Message == "用户取消")
                    {
                        progressBar = null;
                        return Result.Cancelled;
                    }
                    throw;
                }
            }
            foreach (var pb in commandData.Application.GetRibbonPanels(Tab.AddIns).First(rp => rp.Name == "扣件式钢管脚手架").GetItems())// 激活Panel全部Button
                pb.Enabled = true;
            #endregion

            progressBar.Close();
            progressBar = null;

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
