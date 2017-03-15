using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScaffoldTool
{
    static class XC_ScaffoldCreation
    {
        // 扣件悬挑式脚手架XML存取
        internal static void KJSaveOverhangXml(Document doc, XC_KJScaffold scaffold, bool isUppest, bool overWrite)
        {
            scaffold.CalculateLines();
            scaffold.CalculateCorners();
            scaffold.CalculateRows();
            scaffold.CalculateConnects();
            double topOffset = isUppest ? Global.COLUMN_BEYOND_TOP : 0;
            scaffold.CalculateSlantRods(Global.BOTTOM_OVERHANG_BEAM_HEIGHT, topOffset);
            scaffold.CalculateBoards(Global.BOTTOM_OVERHANG_BEAM_HEIGHT);
            scaffold.CalculateOverhangBeams();
            KJScaffoldXmlHelper writableXml = KJScaffoldXmlHelper.GetWritableXmlHelper(doc, overWrite);// 创建Xml本地文件
            Level baseLevel = scaffold.HostGenericModel.LevelSet[0];
            double baseHeight = scaffold.HostGenericModel.startElevation + Global.BOTTOM_OVERHANG_BEAM_HEIGHT;// 填入底部开始创建偏移量
            double topHeight = isUppest ?
                scaffold.HostGenericModel.endElevation + Global.COLUMN_BEYOND_TOP :
                scaffold.HostGenericModel.endElevation;
            // 立杆
            foreach (var pos in scaffold.GetScaffoldAllColumns().Select(point => new XYZ(point.X, point.Y, 0)))
            {
                writableXml.WriteColumnXml(pos, baseHeight, topHeight, scaffold.HostGenericModel);
            }
            // 斜杆
            foreach (Curve c in scaffold.GetScaffoldAllSlantRods())
            {
                using (c)
                    writableXml.WriteSlantRodXml(c, scaffold.HostGenericModel);
            }
            // 横杆
            Transform topToRowStart = Transform.CreateTranslation(new XYZ(0, 0,
                baseHeight + Global.PIPE_BASE_OFFSET - scaffold.HostGenericModel.endElevation));
            double[] offsetArray = GetRowCopyOffsetList(scaffold.HostGenericModel, Global.BOTTOM_OVERHANG_BEAM_HEIGHT).ToArray();
            double topAdd = isUppest ? Global.COLUMN_BEYOND_TOP : 0;
            foreach (Curve c1 in scaffold.GetScaffoldAllHorizontalRods())
            {
                using (c1)
                using (Curve c2 = c1.CreateTransformed(topToRowStart))
                    writableXml.WriteRowXml(c2, offsetArray, scaffold.HostGenericModel, topAdd);
            }
            // 板类（脚手板、挡脚板、安全网）
            double[] arrayOffset2 = GetBoardCopyOffsetList(scaffold.HostGenericModel).ToArray();
            List<double> arrayOffset3 = GetAQWHeightList(scaffold.HostGenericModel, Global.BOTTOM_OVERHANG_BEAM_HEIGHT);// 安全网分层高度集合
            foreach (CurveArray[] cArray in scaffold.GetScaffoldBoards())
            {
                using (cArray[0])
                    writableXml.WriteFootBoardXml(cArray[0], arrayOffset2, scaffold.HostGenericModel);
                using (cArray[1])
                    writableXml.WriteBorderXml(cArray[1].get_Item(0), arrayOffset2, scaffold.HostGenericModel);
                using (cArray[2])
                    writableXml.WriteNetXml(cArray[2].get_Item(0), arrayOffset3, scaffold.HostGenericModel);
            }

            // 悬挑梁
            Transform topToBeamBase = Transform.CreateTranslation(new XYZ(0, 0,
                scaffold.HostGenericModel.startElevation + Global.BOTTOM_OVERHANG_BEAM_HEIGHT - scaffold.HostGenericModel.endElevation));
            foreach (Line l in scaffold.GetScaffoldOverhangBeams())
            {
                using (Curve beamLine = l.CreateTransformed(topToBeamBase))
                {
                    writableXml.WriteOverhangBeam(beamLine, scaffold.HostGenericModel);
                }
            }
            // 标高记录
            writableXml.WriteLevelXml(scaffold.HostGenericModel);
            writableXml.EditQuit();
        }

        // 扣件悬挑式脚手架XML存取补充
        internal static void KJSaveOverhangXmlExtend(Document doc, XC_KJScaffold scaffold)
        {
            scaffold.CalculateLines();
            scaffold.CalculateFixtures();// 连墙件(主要耗时点99%)
            KJScaffoldXmlHelper writableXml = KJScaffoldXmlHelper.GetWritableXmlHelper(doc);// 创建Xml本地文件          
            writableXml.WriteFixtureXml(scaffold.GetScaffoldFixtures());// 连墙件
            writableXml.EditQuit();
        }

        // 扣件落地式脚手架XML存取
        internal static void KJSaveOnGroundXml(Document doc, XC_KJScaffold scaffold, bool overWrite)
        {
            scaffold.CalculateLines();
            scaffold.CalculateCorners();
            scaffold.CalculateRows();
            scaffold.CalculateConnects();
            scaffold.CalculateSlantRods(Global.BOTTOM_PLATE_THICK, Global.COLUMN_BEYOND_TOP);// 填入底部开始创建偏移量

            // 连墙件族文件尚未制作(主要耗时点99%)
            scaffold.CalculateFixtures();

            scaffold.CalculateBoards(Global.BOTTOM_PLATE_THICK);// 填入底部开始创建偏移量
            KJScaffoldXmlHelper writableXml = KJScaffoldXmlHelper.GetWritableXmlHelper(doc, overWrite);// 创建Xml本地文件
            Level baseLevel = scaffold.HostGenericModel.LevelSet[0];
            double baseHeight = scaffold.HostGenericModel.LevelSet[0].Elevation + scaffold.HostGenericModel.LevelSet.Base2BaseOffset + Global.BOTTOM_PLATE_THICK;// 填入底部开始创建偏移量
            double topHeight = scaffold.HostGenericModel.LevelSet[0].Elevation + scaffold.HostGenericModel.LevelSet.Top2BaseOffset + Global.COLUMN_BEYOND_TOP;
            // 立杆 && 垫板
            foreach (XYZ pos in scaffold.GetScaffoldAllColumns().Select(point => new XYZ(point.X, point.Y, 0)))
            {
                writableXml.WriteColumnXml(pos, baseHeight, topHeight, scaffold.HostGenericModel);
                writableXml.WriteBottomPlateXml(pos, baseHeight, scaffold.HostGenericModel);
            }
            // 斜杆
            foreach (Curve c in scaffold.GetScaffoldAllSlantRods())
            {
                using (c)
                    writableXml.WriteSlantRodXml(c, scaffold.HostGenericModel);
            }
            // 横杆
            Transform zeroPlaneTransform = Transform.CreateTranslation(new XYZ(0, 0, Global.PIPE_BASE_OFFSET - scaffold.HostGenericModel.TopCurveList[0].GetEndPoint(0).Z + scaffold.HostGenericModel.startElevation + Global.BOTTOM_PLATE_THICK));// 填入底部开始创建偏移量
            double[] arrayOffset1 = GetRowCopyOffsetList(scaffold.HostGenericModel, Global.BOTTOM_PLATE_THICK).ToArray();
            foreach (Curve c1 in scaffold.GetScaffoldAllHorizontalRods())
            {
                using (c1)
                using (Curve c2 = c1.CreateTransformed(zeroPlaneTransform))
                    writableXml.WriteRowXml(c2, arrayOffset1, scaffold.HostGenericModel, Global.COLUMN_BEYOND_TOP);
            }
            // 板类（脚手板、挡脚板、安全网）
            double[] arrayOffset2 = GetBoardCopyOffsetList(scaffold.HostGenericModel).ToArray();
            List<double> arrayOffset3 = GetAQWHeightList(scaffold.HostGenericModel, Global.BOTTOM_PLATE_THICK);// 安全网分层高度集合
            foreach (CurveArray[] cArray in scaffold.GetScaffoldBoards())
            {
                using (cArray[0])
                    writableXml.WriteFootBoardXml(cArray[0], arrayOffset2, scaffold.HostGenericModel);
                using (cArray[1])
                    writableXml.WriteBorderXml(cArray[1].get_Item(0), arrayOffset2, scaffold.HostGenericModel);
                using (cArray[2])
                    writableXml.WriteNetXml(cArray[2].get_Item(0), arrayOffset3, scaffold.HostGenericModel);
            }

            // 连墙件
            writableXml.WriteFixtureXml(scaffold.GetScaffoldFixtures());

            // 标高记录
            writableXml.WriteLevelXml(scaffold.HostGenericModel);
            writableXml.EditQuit();
        }

        // 扣件脚手架XML读取并翻模
        internal static void KJModelingFromXml(Document doc, IEnumerable<Level> modelingLevel, Transaction trans)
        {
            trans.Start();
            KJScaffoldXmlHelper readableXml = KJScaffoldXmlHelper.GetReadableXmlHelper(doc);

            FamilySymbol columnType = GetCustomType(doc, "星层_扣件式立杆");
            if (!columnType.IsActive)
                columnType.Activate();
            List<MyPipeObj> list1 = readableXml.GetColumnsByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list1.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成立杆……";
            foreach (var item in list1)// 立杆
            {
                FamilyInstance instance = doc.Create.NewFamilyInstance(item.StartPoint, columnType, item.BaseLevel, StructuralType.NonStructural);
                instance.LookupParameter("偏移量").Set(item.Offset);
                instance.LookupParameter("高度").Set(item.Height);
                ModelingCommand.progressBar.Change();
            }
            list1.Clear();

            FamilySymbol slantRodType = GetCustomType(doc, "星层_扣件式斜杆");
            if (!slantRodType.IsActive)
                slantRodType.Activate();
            List<MyPipeObj> list2 = readableXml.GetSlantRodsByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list2.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成斜杆……";
            foreach (var item in list2)// 斜杆
            {
                doc.Create.NewFamilyInstance(Line.CreateBound(item.StartPoint, item.EndPoint), slantRodType, item.BaseLevel, StructuralType.Beam);
                ModelingCommand.progressBar.Change();
            }
            list2.Clear();

            FamilySymbol fixtureRowType = GetCustomType(doc, "星层_连墙件横杆");
            if (!fixtureRowType.IsActive)
                fixtureRowType.Activate();
            FamilySymbol fixtureColumnType = GetCustomType(doc, "星层_连墙件立杆");
            if (!fixtureColumnType.IsActive)
                fixtureColumnType.Activate();
            List<MyPipeObj> list3 = readableXml.GetFixturesByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list3.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成连墙件……";
            foreach (var item in list3)// 连墙件
            {
                if (item.IsHorizontal)
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(Line.CreateBound(item.StartPoint, item.EndPoint), fixtureRowType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                }
                else
                {
                    FamilyInstance instance = doc.Create.NewFamilyInstance(item.StartPoint, fixtureColumnType, item.BaseLevel, StructuralType.NonStructural);
                    instance.LookupParameter("偏移量").Set(item.Offset);
                    instance.LookupParameter("高度").Set(item.Height);
                }
                ModelingCommand.progressBar.Change();
            }
            list3.Clear();

            FamilySymbol rowType = GetCustomType(doc, "星层_48脚手架杆件");
            if (!rowType.IsActive)
                rowType.Activate();
            List<MyPipeObj> list4 = readableXml.GetRowsByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list4.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成横杆……";
            foreach (var item in list4)// 横杆
            {
                FamilyInstance instance = doc.Create.NewFamilyInstance(Line.CreateBound(item.StartPoint, item.EndPoint), rowType, item.BaseLevel, StructuralType.NonStructural);
                instance.LookupParameter("偏移量").Set(item.Offset);
                ModelingCommand.progressBar.Change();
            }
            list4.Clear();

            FloorType floorType1 = GetFloorType(doc, "脚手板", Global.SCAFFOLD_HORIZONTAL_FLOOR_THICK);
            List<MyFloorObj> list5 = readableXml.GetFootBoardsByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list5.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成脚手板……";
            foreach (var item in list5)// 脚手板
            {
                doc.Create.NewFloor(item.CurveArray, floorType1, item.BaseLevel, false, XYZ.BasisZ);
                ModelingCommand.progressBar.Change();
            }
            list5.Clear();

            FamilySymbol floorType2 = GetCustomType(doc, "星层_挡脚板");
            if (!floorType2.IsActive)
                floorType2.Activate();
            List<MyBorderObj> list6 = readableXml.GetBordersByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list6.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成挡脚板……";
            foreach (var item in list6)// 挡脚板
            {
                FamilyInstance instance = doc.Create.NewFamilyInstance(item.Curve, floorType2, item.BaseLevel, StructuralType.NonStructural);
                instance.LookupParameter("偏移量").Set(item.Offset);
                ModelingCommand.progressBar.Change();
            }
            list6.Clear();

            FamilySymbol wallType3 = GetCustomType(doc, "星层_脚手架安全网");
            if (!wallType3.IsActive)
                wallType3.Activate();
            List<MyNetObj> list7 = readableXml.GetNetsByLevels(modelingLevel);
            ModelingCommand.progressBar.Maximum = list7.Count;
            ModelingCommand.progressBar.Value = 0;
            ModelingCommand.progressBar.Tip = "正在生成安全网……";
            foreach (var item in list7)// 安全网
            {
                FamilyInstance instance = doc.Create.NewFamilyInstance(item.Curve, wallType3, item.BaseLevel, StructuralType.NonStructural);
                instance.LookupParameter("高度").Set(item.Height);
                instance.LookupParameter("偏移量").Set(item.Offset);
                ModelingCommand.progressBar.Change();
            }
            list7.Clear();

            // 垫板 || 悬挑梁
            if (readableXml.IsOnGroundScaffoldXml)
            {
                using (FamilySymbol plateType1 = GetBottomPlateType(doc, "垫板", Global.BOTTOM_PLATE_LENGTH, Global.BOTTOM_PLATE_THICK))
                using (Material plateMaterial = GetWoodMaterial(doc))
                {
                    List<XYZ> list8 = readableXml.GetBottomPlates(modelingLevel.First()).ToList();
                    ModelingCommand.progressBar.Maximum = list8.Count;
                    ModelingCommand.progressBar.Value = 0;
                    ModelingCommand.progressBar.Tip = "正在生成垫板……";
                    foreach (var pos in list8)
                    {
                        doc.Create.NewFamilyInstance(pos, plateType1, StructuralType.Footing).LookupParameter("结构材质").Set(plateMaterial.Id);
                        ModelingCommand.progressBar.Change();
                    }
                    list8.Clear();
                }
            }
            else
            {
                using (FamilySymbol overhangBeamType = GetOverhangBeamType(doc, "悬挑梁", Global.BOTTOM_OVERHANG_BEAM_HEIGHT))
                {
                    List<MyBeamObj> list9 = readableXml.GetOverhangBeams(modelingLevel).ToList();
                    ModelingCommand.progressBar.Maximum = list9.Count;
                    ModelingCommand.progressBar.Value = 0;
                    ModelingCommand.progressBar.Tip = "正在生成悬挑梁……";
                    foreach (var item in list9)
                    {
                        doc.Create.NewFamilyInstance(item.Curve, overhangBeamType, item.Level, StructuralType.Beam);
                        ModelingCommand.progressBar.Change();
                    }
                    list9.Clear();
                }
            }
            ModelingCommand.progressBar.Text = "正在等待模型重生成，请稍等……";
            ModelingCommand.progressBar.IsIndeterminate = true;
            trans.Commit();
        }

        internal static FamilySymbol GetCustomType(Document doc, string name)
        {
            Element[] typeArray = new FilteredElementCollector(doc).WhereElementIsElementType().
                Where(elem => elem is FamilySymbol && elem.Name == name).ToArray();
            if (typeArray.Length > 0)
                return typeArray[0] as FamilySymbol;
            Family family;
            if (!doc.LoadFamily(Global.ASSEMBLY_DIRECTORY_PATH + @"\Family\" + name + ".rfa", out family))
            {
                return null;
            }
            return doc.GetElement(family.GetFamilySymbolIds().First()) as FamilySymbol;
        }

        internal static IEnumerable<double> GetRowCopyOffsetList(XC_GenericModel gm, double bottomOffset)
        {
            List<double> result = new List<double>();
            for (double offset = Global.BJ; offset < gm.LevelSet.Height - Global.PIPE_BASE_OFFSET - bottomOffset; offset += Global.BJ)
            {
                result.Add(offset);
            }
            return result;
        }

        internal static IEnumerable<double> GetBoardCopyOffsetList(XC_GenericModel gm)
        {
            List<double> result = new List<double>();
            for (double offset = Global.BJ * Global.SCAFFOLD_FLOOR_SPAN; offset < gm.LevelSet.Height - Global.PIPE_BASE_OFFSET; offset += Global.BJ * Global.SCAFFOLD_FLOOR_SPAN)
            {
                result.Add(offset);
            }
            return result;
        }

        internal static List<double> GetAQWHeightList(XC_GenericModel gm, double bottomOffset)
        {
            List<double> result = new List<double>();
            for (double offset = gm.startElevation + bottomOffset + Global.PIPE_BASE_OFFSET + 0.5 * Global.D; offset < gm.endElevation; offset += Global.BJ)
            {
                result.Add(offset);
            }
            result.RemoveAt(result.Count - 1);
            return result;
        }

        internal static string DIRECTORY_PATH_FOOT = @"C:\ProgramData\Autodesk\RVT 2016\Libraries\China\结构\基础";
        internal static string DIRECTORY_PATH_OVERHANG_BEAM = @"C:\ProgramData\Autodesk\RVT 2016\Libraries\China\结构\框架\钢";

        internal static List<FamilySymbol> GetOrCreateFamilySymbols(Document doc, BuiltInCategory bic, string familyName, string familyDirectory)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Func<Element, bool> func_0 = elem => elem is FamilySymbol;
            List<FamilySymbol> familySymbols = (from o in collector.OfCategory(bic).WhereElementIsElementType().Where(func_0).Cast<FamilySymbol>().ToList()
                                                                           where o.Family != null && o.FamilyName == familyName
                                                                           select o).ToList();
            if (familySymbols.Count != 0)
            {
                return familySymbols;
            }
            string filePath = Path.Combine(familyDirectory, string.Format("{0}.rfa", familyName));
            Family family;
            if (!doc.LoadFamily(filePath, out family))
            {
                return null;
            }
            else
            {
                foreach (ElementId symbolId in family.GetFamilySymbolIds())
                {
                    FamilySymbol familySymbol = doc.GetElement(symbolId) as FamilySymbol;
                    if (familySymbol != null)
                        familySymbols.Add(familySymbol);
                }
            }
            return familySymbols;
        }

        internal static FloorType GetFloorType(Document doc, string name, double thick)
        {
            FloorType[] typeArray = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).OfClass(typeof(FloorType)).Cast<FloorType>().ToArray();
            FloorType[] result = (from o in typeArray where o.Name == name select o).ToArray();
            if (result.Length > 0)
            {
                return result[0];
            }
            FloorType myFloorType = typeArray[0].Duplicate(name) as FloorType;
            ElementId materialId = GetWoodMaterial(doc).Id;
            CompoundStructure cs = CompoundStructure.CreateSingleLayerCompoundStructure(MaterialFunctionAssignment.Structure, thick, materialId);
            cs.EndCap = EndCapCondition.NoEndCap;
            myFloorType.SetCompoundStructure(cs);
            return myFloorType;
        }

        internal static FamilySymbol GetBottomPlateType(Document doc, string name, double length, double thick)
        {
            List<FamilySymbol> typeArray = GetOrCreateFamilySymbols(doc, BuiltInCategory.OST_StructuralFoundation, "基脚 - 矩形", DIRECTORY_PATH_FOOT);
            FamilySymbol[] result = (from o in typeArray where o.Name == name select o).ToArray();
            if (result.Length > 0)
            {
                return result[0];
            }
            FamilySymbol myBottomPlateType = typeArray[0].Duplicate(name) as FamilySymbol;
            myBottomPlateType.LookupParameter("长度").Set(length);
            myBottomPlateType.LookupParameter("宽度").Set(length);
            myBottomPlateType.LookupParameter("厚度").Set(thick);
            return myBottomPlateType;
        }

        internal static FamilySymbol GetOverhangBeamType(Document doc, string name, double height)
        {
            List<FamilySymbol> typeArray = GetOrCreateFamilySymbols(doc, BuiltInCategory.OST_StructuralFraming, "热轧工字钢", DIRECTORY_PATH_OVERHANG_BEAM);
            FamilySymbol[] result = (from o in typeArray where o.Name == name select o).ToArray();
            if (result.Length > 0)
            {
                return result[0];
            }
            FamilySymbol myOverhangBeamType = typeArray[0].Duplicate(name) as FamilySymbol;
            myOverhangBeamType.LookupParameter("d").Set(height);
            return myOverhangBeamType;
        }

        internal static Material GetWoodMaterial(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .First(c => c.Name.Contains("木")) as Material;
        }
    }
}