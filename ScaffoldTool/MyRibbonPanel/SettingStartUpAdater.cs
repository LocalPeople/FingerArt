using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScaffoldTool
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SettingStartUpAdater : IExternalCommand
    {
        //static SettingStartUpAdater()
        //{
        //    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        //}

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Rvt", Environment.CurrentDirectory);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Result result;
            try
            {
                SettingStartUp start = new SettingStartUp();
                result = start.Execute(commandData, ref message, elements);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            return result;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            TaskDialog.Show("Rvt", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Dll", assemblyName.Name + ".dll"));
            return Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Dll", assemblyName.Name + ".dll"));
        }
    }
}
