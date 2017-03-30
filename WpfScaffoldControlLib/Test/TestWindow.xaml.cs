using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XcWpfControlLib.Control;

namespace XcWpfControlLib.Test
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();

            RichItemViewModel[] itemSource = new RichItemViewModel[]
            {
                new TextBoxItemViewModel("布置参数","立杆纵距(m)",1.5),
                new TextBoxItemViewModel("荷载参数","栏杆、挡板类别","栏杆、冲压钢挡脚板"),
                new StringComboBoxItemViewModel("荷载参数","地基土类型","砂土",new string[] { "黏土","砂土","老土","真老土"},StringComboBoxType.Single),
                new StringComboBoxItemViewModel("荷载参数", "复选框类型", "老土;砂土", new string[] { "黏土", "砂土", "老土", "真老土" }, StringComboBoxType.Multiple),
                new ImageComboBoxItemViewModel("荷载参数","图片类型",2,new ImageComboBoxItemViewModel.ImageAttribute[] { new ImageComboBoxItemViewModel.ImageAttribute(1,"图片 1","图片介绍 1","51593661a65fa.jpg"),new ImageComboBoxItemViewModel.ImageAttribute(2,"图片 2", "图片介绍 2", "146397200983.jpg") }),
            };
            itemsControl.ItemsSource = itemSource;
        }
    }
}
