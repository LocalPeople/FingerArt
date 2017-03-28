using System;
using System.Collections.Generic;
using System.Windows;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// ScaffoldWindow1.xaml 的交互逻辑
    /// </summary>
    public partial class ScaffoldWindow1 : Window
    {
        public bool IsBuildOnGround { get { return settingPanel.IsBuildOnGround; } }

        internal ScaffoldWindow1()
        {
            InitializeComponent();
        }

        private double _scaffoldHeight;
        private string _docPathName;
        public ScaffoldWindow1(double height, string docPathName)
        {
            InitializeComponent();

            _scaffoldHeight = height * 0.3048;
            _docPathName = docPathName;

            calculationPanel.ReturnButtonClick += CalculationPanel_ReturnButtonClick;
            calculationPanel.ConfirmButtonClick += CalculationPanel_ConfirmButtonClick;
        }

        #region 事件
        public Dictionary<string, string> StructuralProperties { get; private set; }
        private void CalculationPanel_ConfirmButtonClick(object sender, EventArgs e)
        {
            StructuralProperties = settingPanel.GetStructuralProperties();
            DialogResult = true;
        }

        private void CalculationPanel_ReturnButtonClick(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }

        private void calculationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> keys, values;
            if (settingPanel.GetKeyValueProperties(out keys, out values))
            {
                keys.Add("DSGD");
                values.Add(Math.Round(_scaffoldHeight, 3).ToString());
                calculationPanel.Configure(keys, values, _docPathName, keys.Count <= 28);
                calculationPanel.ShowResult();
            }
            else
            {
                calculationPanel.ShowTip("未生成最新校核结果，请返回点击按钮配置确认，获取最新校核结果……");
            }
        }
        #endregion
    }
}
