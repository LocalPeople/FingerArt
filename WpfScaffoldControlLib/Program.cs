using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XcWpfControlLib
{
    class Program : Application
    {
        [STAThread()]
        static void Main()
        {
            Program app = new Program();
            app.MainWindow = new Test.TestWindow();
            app.MainWindow.ShowDialog();
        }
    }
}
