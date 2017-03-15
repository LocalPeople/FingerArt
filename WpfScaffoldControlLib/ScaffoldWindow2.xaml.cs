using System.Collections.Generic;
using System.Windows;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// ScaffoldWindow2.xaml 的交互逻辑
    /// </summary>
    public partial class ScaffoldWindow2 : Window
    {
        public List<string> Add { get; internal set; }
        public List<string> Delete { get; internal set; }

        internal ScaffoldWindow2()
        {
            InitializeComponent();
        }

        public ScaffoldWindow2(List<string> notYetBuilt, List<string> alreadyBuilt)
        {
            InitializeComponent();

            modelingPanel.SetViewModel(notYetBuilt, alreadyBuilt);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<string> add, delete;

            modelingPanel.GetModelingChanged(out add, out delete);
            Add = add;
            Delete = delete;

            DialogResult = true;
        }
    }
}
