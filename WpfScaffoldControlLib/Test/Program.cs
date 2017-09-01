using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XcWpfControlLib.Test
{
    class Program : Application
    {
        [STAThread()]
        static void Main()
        {
            Program app = new Program();
            app.MainWindow = new TestWindow();
            app.MainWindow.ShowDialog();
        }
    }
}
