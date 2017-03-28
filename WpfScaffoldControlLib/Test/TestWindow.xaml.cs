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
                new ComboBoxItemViewModel("荷载参数","地基土类型","砂土",new string[] { "黏土","砂土","老土","真老土"},ComboBoxType.ComboBox),
                new ComboBoxItemViewModel("荷载参数","图片类型","146397200983.jpg",new string[] { "51593661a65fa.jpg","146397200983.jpg","1463971882999.jpg","壁纸20170228151200.jpg","壁纸20170228151429.jpg","20130505032309_HJhAF.jpeg","Delphox-Designs-iOS-8-Desktop-Wallpaper-5.jpg"},ComboBoxType.ImageComboBox),
            };
            itemsControl.ItemsSource = itemSource;
        }
    }
}
