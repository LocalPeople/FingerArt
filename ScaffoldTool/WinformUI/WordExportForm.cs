using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WordExport
{
    /// <summary>
    /// 设计校核界面类
    /// </summary>
    public partial class WordExportForm : Form
    {
        /// <summary>
        /// 计算书模板原件路径
        /// </summary>
        private string filePath;
        /// <summary>
        /// 计算书本地副本路径
        /// </summary>
        private string filePathCopy;
        private Dictionary<string, string> replaceDictionary;
        private StringBuilder sbForPiecesOfText = new StringBuilder();
        private int piecesOfTextStart = -1;
        private int piecesOfTextEnd = -1;
        public bool IsReadyToModeling { get; private set; }
        private bool needColorSet;
        private string colorValue;
        private Autodesk.Revit.DB.Document _doc;

        public WordExportForm(Autodesk.Revit.DB.Document doc)
        {
            InitializeComponent();
            IsReadyToModeling = false;
            _doc = doc;
        }

        public void Configure(string[] keys, string[] values, bool isScaffoldBuildOnGround)
        {
            if (isScaffoldBuildOnGround)
            {
                filePath = Path.Combine(ScaffoldTool.Global.ASSEMBLY_DIRECTORY_PATH + @"\落地脚手架搭设计算书模板(统一字色).docx");
                filePathCopy = Path.ChangeExtension(_doc.PathName, @"扣件式脚手架搭设计算书.docx");
                replaceDictionary = ReplaceUtil.GetReplaceDictionary(keys, values, Path.Combine(ScaffoldTool.Global.ASSEMBLY_DIRECTORY_PATH + @"\Formula.扣件落地.config"));
            }
            else
            {
                filePath = Path.Combine(ScaffoldTool.Global.ASSEMBLY_DIRECTORY_PATH + @"\悬挑脚手架搭设计算书模板(统一字色).docx");
                filePathCopy = Path.ChangeExtension(_doc.PathName, @"扣件式脚手架搭设计算书.docx");
                replaceDictionary = ReplaceUtil.GetReplaceDictionary(keys, values, Path.Combine(ScaffoldTool.Global.ASSEMBLY_DIRECTORY_PATH + @"\Formula.扣件悬挑.config"));
            }
        }

        #region Microsoft Word模板处理代码段

        private void CreateWordByModel()
        {
            File.Copy(filePath, filePathCopy, true);
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePathCopy, true))
            {
                #region 表格
                foreach (Table table in wordDoc.MainDocumentPart.Document.Body.Elements<Table>())
                {
                    foreach (var para in GetParagraphsInTable(table))
                    {
                        ParagraphHandler(para);
                    }
                }
                #endregion

                #region 段落
                foreach (Paragraph para in wordDoc.MainDocumentPart.Document.Body.Elements<Paragraph>())
                {
                    ParagraphHandler(para);
                }
                #endregion
            }
        }

        private void ParagraphHandler(Paragraph para)
        {
            if (para.InnerText.StartsWith("** "))
            {
                needColorSet = true;
                Regex regex1 = new Regex("[0-9]?SFMZ[0-9]+");
                string key = regex1.Match(para.InnerText).Value;
                colorValue = replaceDictionary[key] == "满足" ? "00FF00" : "FF0000";
            }
            else
                needColorSet = false;
            for (int i = 0; i < para.ChildElements.Count; i++)
            {
                if (para.ChildElements[i].LocalName != "bookmarkStart" && para.ChildElements[i].LocalName != "bookmarkEnd")
                {
                    if (para.ChildElements[i].LocalName == "r" || para.ChildElements[i].LocalName == "smartTag")
                        CoachText(para.ChildElements[i], i);
                    else
                    {
                        i -= ReplaceThenPrint(para);
                    }
                    if (para.ChildElements[i].LocalName == "oMath")
                        OfficeMathHandler1(para.ChildElements[i]);
                    else if (para.ChildElements[i].LocalName == "oMathPara")
                        OfficeMathParaHandler(para.ChildElements[i]);
                }
            }
            ReplaceThenPrint(para);
        }

        private IEnumerable<Paragraph> GetParagraphsInTable(Table table)
        {
            foreach (var child1 in table.ChildElements)
                if (child1.LocalName == "tr")
                    foreach (var child2 in child1.ChildElements)
                        if (child2.LocalName == "tc")
                            foreach (var child3 in child2.ChildElements)
                                if (child3.LocalName == "p")
                                    yield return child3 as Paragraph;
        }

        private void CoachText(OpenXmlElement elem, int index)
        {
            sbForPiecesOfText.Append(elem.InnerText);
            if (piecesOfTextStart < 0)
            {
                piecesOfTextStart = index;
                piecesOfTextEnd = index;
            }
            else
                piecesOfTextEnd = index;
        }

        private int ReplaceThenPrint(OpenXmlElement host)
        {
            int result = 0;
            if (piecesOfTextStart > -1)
            {
                string parseText;
                if (ReplaceUtil.FormulaParser(replaceDictionary, sbForPiecesOfText.ToString(), out parseText) || needColorSet)
                {
                    result = piecesOfTextEnd - piecesOfTextStart;
                    if (host.GetType().ToString().Contains("DocumentFormat.OpenXml.Math"))
                        host.ChildElements[piecesOfTextStart].Elements<DocumentFormat.OpenXml.Math.Text>().First().Text = parseText.ToString();
                    else
                        host.ChildElements[piecesOfTextStart].Elements<Text>().First().Text = parseText.ToString();
                    if (needColorSet)
                        host.ChildElements[piecesOfTextStart].Elements<RunProperties>().First().Color.Val = colorValue;
                    if (piecesOfTextEnd > piecesOfTextStart)
                    {
                        List<OpenXmlElement> elemToRemove = new List<OpenXmlElement>();
                        while (piecesOfTextStart < piecesOfTextEnd)
                            elemToRemove.Add(host.ChildElements[++piecesOfTextStart]);
                        elemToRemove.ForEach(elem => elem.Remove());
                    }
                }
                piecesOfTextStart = -1; piecesOfTextEnd = -1;
                sbForPiecesOfText.Clear();
            }
            return result;
        }

        private void OfficeMathParaHandler(OpenXmlElement elem)
        {
            IEnumerator<OpenXmlElement> enumerator = elem.ChildElements.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.LocalName != "bookmarkStart" && enumerator.Current.LocalName != "bookmarkEnd")
                    if (enumerator.Current.LocalName == "oMath")
                        OfficeMathHandler1(enumerator.Current);
            }
        }

        private void OfficeMathHandler1(OpenXmlElement elem1)
        {
            IEnumerator<OpenXmlElement> enumerator = elem1.ChildElements.GetEnumerator();
            int index = 0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.LocalName != "bookmarkStart" && enumerator.Current.LocalName != "bookmarkEnd")
                    if (enumerator.Current.LocalName == "r" || enumerator.Current.LocalName == "smartTag")
                    {
                        CoachText(enumerator.Current, index);
                    }
                    else
                    {
                        index -= ReplaceThenPrint(elem1);
                        if (enumerator.Current.LocalName == "d")
                            OfficeMathHandler1(enumerator.Current.ChildElements[1]);
                        if (enumerator.Current.ChildElements.Count >= 3)
                        {
                            OfficeMathHandler1(enumerator.Current.ChildElements[1]);
                            OfficeMathHandler1(enumerator.Current.ChildElements[2]);
                        }
                    }
                index++;
            }
            ReplaceThenPrint(elem1);
        }

        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // 关闭窗口 & 生成计算书
        private void button3_Click(object sender, EventArgs e)
        {
            this.IsReadyToModeling = true;
            CreateWordByModel();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void WordExportForm_Load(object sender, EventArgs e)
        {
            bool isValid = true;
            this.SuspendLayout();
            richTextBox1.Text = "";
            StringBuilder sbForTemp = new StringBuilder();
            foreach (var str in replaceDictionary.Keys.Where(str => str.Contains("SFMZ")))
            {
                sbForTemp.Append(replaceDictionary[str.Replace("SFMZ", "JGBL")]);
                while (sbForTemp.Length <= 24)
                    sbForTemp.Append("…");
                if (replaceDictionary[str] == "不满足")
                {
                    richTextBox1.SelectionColor = System.Drawing.Color.Red;
                    isValid = false;
                }
                else
                    richTextBox1.SelectionColor = System.Drawing.Color.Green;
                richTextBox1.AppendText(sbForTemp.ToString() + replaceDictionary[str] + "！\r\n");
                sbForTemp.Clear();
            }
            richTextBox1.SelectionColor = System.Drawing.Color.Black;
            richTextBox1.SelectionFont = new System.Drawing.Font("宋体", 10, System.Drawing.FontStyle.Italic);
            if (isValid)
            {
                this.button3.Enabled = true;
                richTextBox1.AppendText("\r\n脚手架参数满足计算要求，请点击按钮前往建模……");
            }
            else
            {
                this.button3.Enabled = false;
                richTextBox1.AppendText("\r\n脚手架参数不满足计算要求，请点击按钮返回设置……");
            }
            this.ResumeLayout();
        }
    }
}
