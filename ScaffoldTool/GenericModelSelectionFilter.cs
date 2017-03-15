using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace ScaffoldTool
{
    internal class GenericModelSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is FamilyInstance && elem.Category.Name == "常规模型";
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}