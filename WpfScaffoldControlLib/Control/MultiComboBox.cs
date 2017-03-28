using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace XcWpfControlLib.Control
{
    class MultiComboBox : ComboBox
    {
        static MultiComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiComboBox), new FrameworkPropertyMetadata(typeof(MultiComboBox)));
        }
    }
}
