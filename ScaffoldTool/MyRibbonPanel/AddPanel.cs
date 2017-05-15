using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System.IO;

namespace ScaffoldTool.MyRibbonPanel
{
    public class AddPanel : IExternalApplication
    {
        private RibbonButton _calculationButton, _modelingButton;

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ViewActivated -= Application_ViewActivated;// 取消订阅文档加载完成事件处理程序
            application.ControlledApplication.DocumentChanged -= ControlledApplication_DocumentChanged;

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("扣件式钢管脚手架");
            ribbonPanel.AddItem(new PushButtonData("ButtonSettingUp", "\n\n参数\n设置", Path.Combine(Global.ASSEMBLY_DIRECTORY_PATH, "ScaffoldTool.dll"), "ScaffoldTool.SettingStartUp"));
            _calculationButton = ribbonPanel.AddItem(new PushButtonData("ButtonCalculationBook", "\n\n导出\n计算书", Path.Combine(Global.ASSEMBLY_DIRECTORY_PATH, "ScaffoldTool.dll"), "ScaffoldTool.CalculationCommand")) as RibbonButton;
            _modelingButton = ribbonPanel.AddItem(new PushButtonData("ButtonModeling", "\n\n智能\n生成模型", Path.Combine(Global.ASSEMBLY_DIRECTORY_PATH, "ScaffoldTool.dll"), "ScaffoldTool.ModelingCommand")) as RibbonButton;
            ribbonPanel = application.CreateRibbonPanel("盘扣式钢管脚手架");
            ribbonPanel.AddItem(new PushButtonData("PKButtonOneStepToSet", "\n\n一键\n创建", Path.Combine(Global.ASSEMBLY_DIRECTORY_PATH, "ScaffoldTool.dll"), typeof(RequireScaffoldParam).FullName));
            application.ViewActivated += Application_ViewActivated;// 订阅文档加载完成事件处理程序
            application.ControlledApplication.DocumentChanged += ControlledApplication_DocumentChanged;

            return Result.Succeeded;
        }

        private void ControlledApplication_DocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (!(e.GetTransactionNames().Contains("ScaffoldTool_ModelingCommand") || e.GetTransactionNames().Contains("ScaffoldTool_DeletingCommand")))
                return;
            if (e.Operation == UndoOperation.TransactionUndone)
            {
                // 在这里处理需要同步的用户回退事件
                KJScaffoldXmlHelper.Undo(e.GetDocument());
            }
            else if (e.Operation == UndoOperation.TransactionRedone)
            {
                // 在这里处理需要同步的用户前进事件
                KJScaffoldXmlHelper.Redone(e.GetDocument());
            }
        }

        private void Application_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
        {
            _calculationButton.Enabled = false;// 默认后两个按钮处于未激活状态
            _modelingButton.Enabled = false;
            if (File.Exists(Path.ChangeExtension(e.Document.PathName, @"扣件式脚手架搭设计算书.docx")))
            {
                _calculationButton.Enabled = true;
            }
            if (File.Exists(Path.ChangeExtension(e.Document.PathName, @"扣件式分层构件.xml")))
            {
                _modelingButton.Enabled = true;
            }
            KJScaffoldXmlHelper.Clear(e.Document);
        }
    }
}
