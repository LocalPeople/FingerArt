using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.IO;

namespace ScaffoldTool
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CalculationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 打开Office Word指定路径文件
            // Interface used to open Microsoft Office Word
            Microsoft.Office.Interop.Word.Application m_word = new Microsoft.Office.Interop.Word.Application();
            Object filefullname = Path.ChangeExtension(commandData.Application.ActiveUIDocument.Document.PathName, @"扣件式脚手架搭设计算书.docx");
            Object confirmConversions = Type.Missing;
            Object readOnly = Type.Missing;
            Object addToRecentFiles = Type.Missing;
            Object passwordDocument = Type.Missing;
            Object passwordTemplate = Type.Missing;
            Object revert = Type.Missing;
            Object writePasswordDocument = Type.Missing;
            Object writePasswordTemplate = Type.Missing;
            Object format = Type.Missing;
            Object encoding = Type.Missing;
            Object visible = Type.Missing;
            Object openConflictDocument = Type.Missing;
            Object openAndRepair = Type.Missing;
            Object documentDirection = Type.Missing;
            Object noEncodingDialog = Type.Missing;
            m_word.Documents.Open(ref filefullname,
                        ref confirmConversions, ref readOnly, ref addToRecentFiles,
                        ref passwordDocument, ref passwordTemplate, ref revert,
                        ref writePasswordDocument, ref writePasswordTemplate,
                        ref format, ref encoding, ref visible, ref openConflictDocument,
                        ref openAndRepair, ref documentDirection, ref noEncodingDialog
                        );
            m_word.Visible = true;
            m_word.Activate();

            return Result.Succeeded;
        }
    }
}
