using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Media;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// CalculationPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CalculationPanel : UserControl
    {
        public CalculationPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 计算书模板原件路径
        /// </summary>
        private string _filePath;
        /// <summary>
        /// 计算书本地副本路径
        /// </summary>
        private string _filePathCopy;
        private Dictionary<string, string> _replaceDictionary;
        private static string RootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        /// <summary>
        /// 配置校核界面数据
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="docPathName"></param>
        /// <param name="isScaffoldBuildOnGround"></param>
        public void Configure(List<string> keys, List<string> values, string docPathName, bool isScaffoldBuildOnGround)
        {
            if (isScaffoldBuildOnGround)
            {
                _filePath = Path.Combine(RootPath + @"\落地脚手架搭设计算书模板(统一字色).docx");
                _filePathCopy = Path.ChangeExtension(docPathName, @"扣件式脚手架搭设计算书.docx");
                _replaceDictionary = ControlLibraryUtils.ReplaceUtil.GetReplaceDictionary(keys, values, Path.Combine(RootPath + @"\Formula.扣件落地.config"));
            }
            else
            {
                _filePath = Path.Combine(RootPath + @"\悬挑脚手架搭设计算书模板(统一字色).docx");
                _filePathCopy = Path.ChangeExtension(docPathName, @"扣件式脚手架搭设计算书.docx");
                _replaceDictionary = ControlLibraryUtils.ReplaceUtil.GetReplaceDictionary(keys, values, Path.Combine(RootPath + @"\Formula.扣件悬挑.config"));
            }
        }

        #region 事件
        public event EventHandler ReturnButtonClick;
        public event EventHandler ConfirmButtonClick;

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            if (ReturnButtonClick != null)
                ReturnButtonClick(this, new EventArgs());
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            CreateWordByModel();
            if (ConfirmButtonClick != null)
                ConfirmButtonClick(this, new EventArgs());
        }
        #endregion

        #region 界面交互函数
        private Brush _bingoBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 75, 191, 20));
        private Brush _failureBrush = Brushes.IndianRed;
        public void ShowResult()
        {
            List<CalculationViewModel> viewModels = new List<CalculationViewModel>();
            int number = 1;
            bool isValid = true;
            foreach (var str in _replaceDictionary.Keys.Where(str => str.Contains("SFMZ")))
            {
                CalculationViewModel vm = new CalculationViewModel
                {
                    Number = number++,
                    Name = _replaceDictionary[str.Replace("SFMZ", "JGBL")],
                    State = _replaceDictionary[str]
                };
                if (vm.State == "不满足")
                    isValid = false;
                viewModels.Add(vm);
            }
            DataContext = viewModels;
            if (isValid)
            {
                textBox1.Text = "脚手架参数满足计算要求，请点击按钮前往建模……";
                textBox1.Foreground = _bingoBrush;
                confirmBtn.IsEnabled = true;
            }
            else
            {
                textBox1.Text = "脚手架参数不满足计算要求，请点击按钮返回设置……";
                textBox1.Foreground = _failureBrush;
                confirmBtn.IsEnabled = false;
            }
        }

        public void ShowTip(string tip)
        {
            textBox1.Text = tip;
            textBox1.Foreground = _failureBrush;
            confirmBtn.IsEnabled = false;
        }
        #endregion

        #region Microsoft Word模板处理代码段
        private StringBuilder _sbForPiecesOfText = new StringBuilder();
        private int _piecesOfTextStart = -1;
        private int _piecesOfTextEnd = -1;
        private bool _needColorSet;
        private string _colorValue;
        private void CreateWordByModel()
        {
            File.Copy(_filePath, _filePathCopy, true);
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(_filePathCopy, true))
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
                _needColorSet = true;
                Regex regex1 = new Regex("[0-9]?SFMZ[0-9]+");
                string key = regex1.Match(para.InnerText).Value;
                _colorValue = _replaceDictionary[key] == "满足" ? "00FF00" : "FF0000";
            }
            else
                _needColorSet = false;
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
                        OfficeMathHandler(para.ChildElements[i]);
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
            _sbForPiecesOfText.Append(elem.InnerText);
            if (_piecesOfTextStart < 0)
            {
                _piecesOfTextStart = index;
                _piecesOfTextEnd = index;
            }
            else
                _piecesOfTextEnd = index;
        }

        private int ReplaceThenPrint(OpenXmlElement host)
        {
            int result = 0;
            if (_piecesOfTextStart > -1)
            {
                string parseText;
                if (ControlLibraryUtils.ReplaceUtil.FormulaParser(_replaceDictionary, _sbForPiecesOfText.ToString(), out parseText) || _needColorSet)
                {
                    result = _piecesOfTextEnd - _piecesOfTextStart;
                    if (host.GetType().ToString().Contains("DocumentFormat.OpenXml.Math"))
                        host.ChildElements[_piecesOfTextStart].Elements<DocumentFormat.OpenXml.Math.Text>().First().Text = parseText.ToString();
                    else
                        host.ChildElements[_piecesOfTextStart].Elements<Text>().First().Text = parseText.ToString();
                    if (_needColorSet)
                        host.ChildElements[_piecesOfTextStart].Elements<RunProperties>().First().Color.Val = _colorValue;
                    if (_piecesOfTextEnd > _piecesOfTextStart)
                    {
                        List<OpenXmlElement> elemToRemove = new List<OpenXmlElement>();
                        while (_piecesOfTextStart < _piecesOfTextEnd)
                            elemToRemove.Add(host.ChildElements[++_piecesOfTextStart]);
                        elemToRemove.ForEach(elem => elem.Remove());
                    }
                }
                _piecesOfTextStart = -1; _piecesOfTextEnd = -1;
                _sbForPiecesOfText.Clear();
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
                        OfficeMathHandler(enumerator.Current);
            }
        }

        private void OfficeMathHandler(OpenXmlElement elem1)
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
                            OfficeMathHandler(enumerator.Current.ChildElements[1]);
                        if (enumerator.Current.ChildElements.Count >= 3)
                        {
                            OfficeMathHandler(enumerator.Current.ChildElements[1]);
                            OfficeMathHandler(enumerator.Current.ChildElements[2]);
                        }
                    }
                index++;
            }
            ReplaceThenPrint(elem1);
        }
        #endregion
    }

    #region 数据模板选择器
    //internal class CalculationItemTemplateSelector : DataTemplateSelector
    //{
    //    public DataTemplate ReasonableDataTemplate { get; set; }
    //    public DataTemplate UnreasonableDataTemplate { get; set; }

    //    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    //    {
    //        CalculationViewModel vm = item as CalculationViewModel;
    //        if (vm.State == "满足")
    //            return ReasonableDataTemplate;
    //        else if (vm.State == "不满足")
    //            return UnreasonableDataTemplate;
    //        return base.SelectTemplate(item, container);
    //    }
    //}
    #endregion

    /// <summary>
    /// 计算校核数据模型
    /// </summary>
    internal class CalculationViewModel
    {
        private int _number;
        private string _name;
        private string _state;

        #region 构造函数
        internal CalculationViewModel()
        {
        }
        #endregion

        public int Number
        {
            get { return _number; }
            set { _number = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }
    }
}
