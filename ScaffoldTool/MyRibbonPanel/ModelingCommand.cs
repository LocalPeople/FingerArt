using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XcWpfControlLib.WpfScaffoldControlLib;

namespace ScaffoldTool
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //[Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.UsingCommandData)]
    public class ModelingCommand : IExternalCommand
    {
        public static XcWpfControlLib.ProgressArgs progressBar;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            IEnumerable<Level> modelingLevel, deleteLevel;

            List<string> alreadyBuilt, notYetBuilt;
            KJScaffoldXmlHelper.GetBuildInfo(doc, out alreadyBuilt, out notYetBuilt);
            ScaffoldWindow2 window = new ScaffoldWindow2(notYetBuilt, alreadyBuilt);
            //UIApplication app = new UIDocument(doc).Application;
            //app.PostCommand(RevitCommandId.LookupCommandId("ButtonModeling"));
            if (window.ShowDialog() == false)
                #region 界面
                return Result.Cancelled;

            FilteredElementCollector filter = new FilteredElementCollector(doc).OfClass(typeof(Level));
            modelingLevel = filter.Cast<Level>().Where(elem => window.Add.Contains(elem.Name)).OrderBy(elem => elem.Elevation);
            deleteLevel = filter.Cast<Level>().Where(elem => window.Delete.Contains(elem.Name)).OrderBy(elem => elem.Elevation);

            if (window != null)
                window.Close();
            #endregion

            progressBar = XcWpfControlLib.ProgressBarWindow.ProgressShow();

            #region 后台处理
            // 创建脚手架模型
            if (modelingLevel.Count() > 0)
                using (Transaction trans = new Transaction(doc, "ScaffoldTool_ModelingCommand"))
                {
                    try
                    {
                        XC_ScaffoldCreation.KJModelingFromXml(doc, modelingLevel, trans);
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
            // 删除脚手架模型
            else
                using (Transaction trans = new Transaction(doc, "ScaffoldTool_DeletingCommand"))
                {
                }

            if (progressBar.IsCancel)
            {
                progressBar.Close();
                progressBar = null;
                return Result.Cancelled;
            }

            // 修改分层模型状态
            KJScaffoldXmlHelper.UpdateBuildInfo(doc, modelingLevel.Select(o => o.Name), deleteLevel.Select(o => o.Name));
            #endregion

            progressBar.Close();
            progressBar = null;


            return Result.Succeeded;
        }
    }
}
