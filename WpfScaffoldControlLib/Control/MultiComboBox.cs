using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace XcWpfControlLib.Control
{
    public class MultiComboBox : ComboBox
    {
        static MultiComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiComboBox), new FrameworkPropertyMetadata(typeof(MultiComboBox)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ListBox listBox = Template.FindName("PART_ListBox", this) as ListBox;
            if (listBox != null)
            {
                listBox.SelectionChanged += ListBox_SelectionChanged;
                listBox.Loaded += ListBox_Loaded;
            }
        }

        private bool _isInit = true;
        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInit)
            {
                _isInit = false;
                ListBox listBox = (ListBox)sender;
                SortedSet<string> textSet = new SortedSet<string>();
                foreach (var text in Text.Split(';'))
                {
                    textSet.Add(text);
                }
                foreach (var item in listBox.Items)
                {
                    if (textSet.Contains(item.ToString()))
                    {
                        listBox.SelectedItems.Add(item);
                    }
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            StringBuilder sb = new StringBuilder();
            foreach (var item in listBox.SelectedItems)
            {
                sb.Append(item.ToString()).Append(";");
            }
            Text = sb.ToString().TrimEnd(';');
        }
    }
}
