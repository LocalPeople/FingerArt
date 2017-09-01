using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace XcWpfControlLib.Control
{
    public class ImageComboBox : ComboBox
    {
        public string ImageDescription
        {
            get { return (string)GetValue(ImageDescriptionProperty); }
            set { SetValue(ImageDescriptionProperty, value); }
        }

        public static readonly DependencyProperty ImageDescriptionProperty =
            DependencyProperty.Register("ImageDescription", typeof(string), typeof(ImageComboBox), new PropertyMetadata(string.Empty));

        string displayProperty = string.Empty;
        public string DisplayProperty { set { displayProperty = value; } }

        static ImageComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageComboBox), new FrameworkPropertyMetadata(typeof(ImageComboBox)));
            SelectedItemProperty.OverrideMetadata(typeof(ImageComboBox), new FrameworkPropertyMetadata(SelectedItemChangedCallback));
        }

        private static void SelectedItemChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ImageComboBox imageComboBox = d as ImageComboBox;
                if (!string.IsNullOrEmpty(imageComboBox.displayProperty))
                {
                    PropertyInfo property = e.NewValue.GetType().GetProperty(imageComboBox.displayProperty);
                    imageComboBox.ImageDescription = property.GetValue(e.NewValue, null).ToString();
                }
                else
                {
                    imageComboBox.ImageDescription = e.NewValue.ToString();
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ListBox listBox = Template.FindName("PART_ListBox", this) as ListBox;
            if (listBox != null)
            {
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SelectedItem = e.AddedItems[0];
            }
            e.Handled = true;
        }
    }
}
