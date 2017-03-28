using System;
using System.Windows;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// SettingPanelAddDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPanelAddDialog : Window
    {
        public string NewItem { get; set; }

        internal SettingPanelAddDialog()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox1.Focus();
        }
    }
}
