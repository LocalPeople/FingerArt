using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPanel : UserControl
    {
        private SettingViewModel[] _viewModel;
        private List<SettingViewModel> _visibleViewModel;
        internal const string INT_ERROR_TIP = "请输入整数！";
        internal const string DOUBLE_ERROR_TIP = "请输入数值！";
        internal const string STRING_ERROR_TIP = "请输入文字！";
        private bool _isSettingSaved;

        public SettingPanel()
        {
            InitializeComponent();

            // 设置参数列表数据源板初始化
            _viewModel = SettingViewModel.GetAllSettingProperties(OnViewModelChanged);
            _visibleViewModel = new List<SettingViewModel>();
            for (int i = 0; i < SettingViewModel.OnGroundPropertiesCount; i++)
                _visibleViewModel.Add(_viewModel[i]);
            DataContext = _visibleViewModel;

            // 项目地区下拉框数据源初始化
            comboBox1.ItemsSource = _comboBoxItems;

            errorProvider1.Visibility = Visibility.Hidden;
            stackpanel1.Visibility = Visibility.Hidden;

            _isSettingSaved = false;
        }

        private void OnViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
                _isSettingSaved = false;
        }

        private Brush _bingoBrush = new SolidColorBrush(Color.FromArgb(255, 75, 191, 20));
        private Brush _failureBrush = Brushes.IndianRed;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            errorProvider1.Visibility = Visibility.Hidden;
            stackpanel1.Visibility = Visibility.Hidden;
            tipText1.Text = string.Empty;
            bool isWrong = false;
            if (string.IsNullOrWhiteSpace(comboBox1.Text))
            {
                errorProvider1.Visibility = Visibility.Visible;
                tipText1.Text = "项目所在地未填写";
                isWrong = true;
            }
            if (!SettingViewModel.IsValidate(_viewModel))
            {
                if (!isWrong)
                {
                    tipText1.Text = "部分参数类型不符";
                    isWrong = true;
                }
                else
                    tipText1.Text += " / 部分参数类型不符";
            }
            if (isWrong)
            {
                tipText1.Foreground = _failureBrush;
            }
            else
            {
                tipText1.Text = "设置已保存，请查看设计校核";
                tipText1.Foreground = _bingoBrush;
                SaveSetting(string.Format("{0}\\xc.{1}.scaffold.config", RootPath, comboBox1.Text));
                _isSettingSaved = true;
            }
            stackpanel1.Visibility = Visibility.Visible;
            itemsControl1.Items.Refresh();
        }

        private void ControlExceptTipText_GotFocus(object sender, RoutedEventArgs e)
        {
            stackpanel1.Visibility = Visibility.Hidden;
        }

        #region 项目所在地下拉框相关行为函数
        private static string RootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private List<string> _comboBoxItems = new List<string>();
        /// <summary>
        /// 本地保存设置参数文档函数
        /// </summary>
        private void SaveSetting(string path)
        {
            XDocument xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("configuration",
                        new XElement("appSettings")));
            XElement appSettings = xDoc.Element("configuration").Element("appSettings");
            foreach (var item in _viewModel)
            {
                if (item.IsSeparator) continue;
                appSettings.Add(new XElement("add", new XAttribute("key", item.LongName.TrimEnd('：')), new XAttribute("value", item.Value)));
            }
            xDoc.Save(path);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _comboBoxItems.Clear();// 清除上次保存的下拉项
            foreach (var configFileInfo in new DirectoryInfo(RootPath).GetFiles("xc.*.scaffold.config"))
            {
                _comboBoxItems.Add(configFileInfo.Name.Split('.')[1]);
            }
            _comboBoxItems.Add("添加项目地区…");
            comboBox1.Items.Refresh();
        }

        private SettingPanelAddDialog _addDialog;
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox1.SelectedItem as string == "添加项目地区…")
            {
                if (_addDialog == null || _addDialog.DialogResult.HasValue)
                {
                    _addDialog = new SettingPanelAddDialog();
                    _addDialog.Owner = Window.GetWindow(this);
                }
                _addDialog.ShowDialog();
                if (!string.IsNullOrWhiteSpace(_addDialog.NewItem))
                {
                    _comboBoxItems.Insert(0, _addDialog.NewItem);
                    SaveSetting(string.Format("{0}\\xc.{1}.scaffold.config", RootPath, _addDialog.NewItem));
                    comboBox1.Items.Refresh();
                    comboBox1.SelectedIndex = 0;
                }
                else
                    comboBox1.SelectedIndex = -1;
            }
            else if (comboBox1.SelectedIndex >= 0 && comboBox1.SelectedIndex < comboBox1.Items.Count - 1)
            {
                System.Configuration.ExeConfigurationFileMap configFile = new System.Configuration.ExeConfigurationFileMap();
                configFile.ExeConfigFilename = string.Format("{0}\\xc.{1}.scaffold.config", RootPath, _comboBoxItems[comboBox1.SelectedIndex]);
                System.Configuration.Configuration cfa =
                    System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configFile, System.Configuration.ConfigurationUserLevel.None);
                for (int i = 0; i < _viewModel.Length; i++)
                {
                    if (_viewModel[i].IsSeparator) continue;
                    _viewModel[i].Value = cfa.AppSettings.Settings[_viewModel[i].LongName.TrimEnd('：')].Value;
                }
                itemsControl1.Items.Refresh();
            }
        }
        #endregion

        #region 更改脚手架设计相关行为函数
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            for (int i = SettingViewModel.OnGroundPropertiesCount; i < _viewModel.Length; i++)
                _visibleViewModel.Add(_viewModel[i]);
            itemsControl1.Items.Refresh();
            _isSettingSaved = false;
            _isBuildOnGrond = false;
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_rbInitial)
            {
                _rbInitial = false;
                return;
            }
            _visibleViewModel.RemoveRange(SettingViewModel.OnGroundPropertiesCount, _viewModel.Length - SettingViewModel.OnGroundPropertiesCount);
            itemsControl1.Items.Refresh();
            _isSettingSaved = false;
            _isBuildOnGrond = true;
        }

        private bool _isBuildOnGrond = true;
        internal bool IsBuildOnGround { get { return _isBuildOnGrond; } }
        private bool _rbInitial = true;
        #endregion

        #region 界面交互函数
        internal bool GetKeyValueProperties(out List<string> keys, out List<string> values)
        {
            keys = new List<string>();
            values = new List<string>();
            if (_isSettingSaved)
            {
                foreach (var item in _visibleViewModel)
                {
                    if (item.IsSeparator) continue;
                    keys.Add(item.ShortName);
                    values.Add(item.Value);
                }
                keys.Add("XMDZ");
                values.Add(comboBox1.Text);
            }
            return _isSettingSaved;
        }

        public Dictionary<string, string> GetStructuralProperties()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add(_viewModel[1].ShortName, _viewModel[1].Value);
            result.Add(_viewModel[2].ShortName, _viewModel[2].Value);
            result.Add(_viewModel[3].ShortName, _viewModel[3].Value);
            result.Add(_viewModel[5].ShortName, _viewModel[5].Value);
            result.Add(_viewModel[6].ShortName, _viewModel[6].Value);
            result.Add(_viewModel[24].ShortName, _viewModel[24].Value);
            return result;
        }
        #endregion
    }

    #region 数据模板选择器
    public class SettingItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultDataTemplate { get; set; }
        public DataTemplate SeparatorDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            SettingViewModel vm = item as SettingViewModel;
            if (!vm.IsSeparator)
                return DefaultDataTemplate;
            else
                return SeparatorDataTemplate;
        }
    }
    #endregion

    /// <summary>
    /// 设置参数数据模型
    /// </summary>
    public class SettingViewModel : INotifyPropertyChanged
    {
        private string _shortName;
        private string _longName;
        private string _value;
        private Visibility _error;
        private string _errorDesription;
        internal static readonly int OnGroundPropertiesCount = 28;

        #region 构造函数
        internal SettingViewModel(string shortName, string longName, object value)
        {
            IsSeparator = false;
            _shortName = shortName;
            _longName = longName;
            _value = value.ToString();
            _error = Visibility.Hidden;
            Type valueType = value.GetType();
            if (valueType == typeof(int))
                _errorDesription = SettingPanel.INT_ERROR_TIP;
            else if (valueType == typeof(double))
                _errorDesription = SettingPanel.DOUBLE_ERROR_TIP;
            else if (valueType == typeof(string))
                _errorDesription = SettingPanel.STRING_ERROR_TIP;
        }

        internal SettingViewModel(string separator)
        {
            IsSeparator = true;
            _longName = separator;
        }
        #endregion

        #region 属性成员
        public bool IsSeparator { get; private set; }

        public string ShortName
        {
            get
            {
                return _shortName;
            }
            set
            {
                _shortName = value;
            }
        }

        public string LongName
        {
            get
            {
                return _longName;
            }
            set
            {
                _longName = value;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public Visibility Error
        {
            get { return _error; }
        }

        public string ErrorDesription
        {
            get { return _errorDesription; }
        }
        #endregion

        #region 事件
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region 静态方法
        public static SettingViewModel[] GetAllSettingProperties(PropertyChangedEventHandler handler = null)
        {
            return new SettingViewModel[]
            {
                new SettingViewModel("布置参数"),
                new SettingViewModel("LGZJ","立杆纵距(m)",1.5){ PropertyChanged=handler},
                new SettingViewModel("LGHJ","立杆横距(m)",0.8){ PropertyChanged=handler},
                new SettingViewModel("BJ","步距(m)",1.8){ PropertyChanged=handler},
                new SettingViewModel("LQJBS","连墙件步数",2){ PropertyChanged=handler},
                new SettingViewModel("LQJKS","连墙件跨数",2){ PropertyChanged=handler},
                new SettingViewModel("NJJQ","内排架距墙长度(mm)",300){ PropertyChanged=handler},
                new SettingViewModel("荷载参数"),
                new SettingViewModel("ZJXS","扣件抗滑承载力系数",0.8){ PropertyChanged=handler},
                new SettingViewModel("JSJYT","脚手架用途","结构脚手架"){ PropertyChanged=handler},
                new SettingViewModel("SGHZ","施工荷载均布参数(kN/m2)",3){ PropertyChanged=handler},
                new SettingViewModel("SGCS","同时施工层数",2){ PropertyChanged=handler},
                new SettingViewModel("JBFY","基本风压(kN/m2)",0.3){ PropertyChanged=handler},
                new SettingViewModel("LGFY","立杆稳定性时风压高度变化系数",0.65){ PropertyChanged=handler},
                new SettingViewModel("LQJFY","连墙件强度时风压高度变化系数",0.65){ PropertyChanged=handler},
                new SettingViewModel("TXXS","风荷载体型系数",1.3){ PropertyChanged=handler},
                new SettingViewModel("LGZZ","每米立杆数承受的结构自重(kN)",0.1295){ PropertyChanged=handler},
                new SettingViewModel("JSBLB","脚手板类别","冲压钢脚手架"){ PropertyChanged=handler},
                new SettingViewModel("JSBZZ","脚手板自重标准值(kN/m2)",0.3){ PropertyChanged=handler},
                new SettingViewModel("DJBLB","栏杆、挡板类别","栏杆、冲压钢挡脚板"){ PropertyChanged=handler},
                new SettingViewModel("DJBZZ","栏杆、挡脚板自重标准值(kN/m2)",0.16){ PropertyChanged=handler},
                new SettingViewModel("AQWZZ","安全设施与安全网自重标准值(kN/m2)",0.005){ PropertyChanged=handler},
                new SettingViewModel("DJTLX","地基土类型","砂土"){ PropertyChanged=handler},
                new SettingViewModel("DJCZL","地基承载力标准值(kPa)",140){ PropertyChanged=handler},
                new SettingViewModel("JCMJ","基础底面扩展面积(m2)",0.09){ PropertyChanged=handler},
                new SettingViewModel("JCXS","基础降低系数",1){ PropertyChanged=handler},
                new SettingViewModel("JSCDXS","计算长度系数",1.5){ PropertyChanged=handler},
                new SettingViewModel("LGWDXS","轴心受压立杆的稳定系数",0.188){ PropertyChanged=handler},
                new SettingViewModel("悬挑参数"),
                new SettingViewModel("1DSGD","该分段顶部高度(m)",14.4){ PropertyChanged=handler},
                new SettingViewModel("XGXH","型钢型号",14){ PropertyChanged=handler},
                new SettingViewModel("HNTBH","楼板混凝土标号","C40"){ PropertyChanged=handler},
                new SettingViewModel("JMMJ","型钢截面面积(cm2)",21.5){ PropertyChanged=handler},
                new SettingViewModel("LLZL","型钢理论质量(kg/m)",16.9){ PropertyChanged=handler},
                new SettingViewModel("ZDBJ","转动半径ix(cm)",5.75){ PropertyChanged=handler},
                new SettingViewModel("JMML","截面模量Wx(cm3)",102){ PropertyChanged=handler},
                new SettingViewModel("ZDGL","转动惯量Ix(cm4)",712){ PropertyChanged=handler},
                new SettingViewModel("1LQJFY","本段顶部风压高度变化系数",0.65){ PropertyChanged=handler},
                new SettingViewModel("MGXTBZ","锚固段长度与悬挑段长度比值",1.3){ PropertyChanged=handler},
                new SettingViewModel("HNTKLQD","混凝土抗拉强度(N/mm2)",1.71){ PropertyChanged=handler},
                new SettingViewModel("HNTKYQD","混凝土抗压强度(N/mm2)",19.2){ PropertyChanged=handler},
            };
        }

        internal static bool IsValidate(SettingViewModel[] viewModel)
        {
            bool isValidatePass = true;
            for (int i = 0; i < viewModel.Length; i++)
            {
                if (viewModel[i].ErrorDesription == SettingPanel.INT_ERROR_TIP)
                {
                    int result;
                    if (!int.TryParse(viewModel[i]._value, out result))
                    {
                        viewModel[i]._error = Visibility.Visible;
                        isValidatePass = false;
                        continue;
                    }
                }
                else if (viewModel[i].ErrorDesription == SettingPanel.DOUBLE_ERROR_TIP)
                {
                    double result;
                    if (!double.TryParse(viewModel[i]._value, out result))
                    {
                        viewModel[i]._error = Visibility.Visible;
                        isValidatePass = false;
                        continue;
                    }
                }
                else if (viewModel[i].ErrorDesription == SettingPanel.STRING_ERROR_TIP)
                {
                    double result;
                    if (double.TryParse(viewModel[i]._value, out result))
                    {
                        viewModel[i]._error = Visibility.Visible;
                        isValidatePass = false;
                        continue;
                    }
                }
                viewModel[i]._error = Visibility.Hidden;
            }
            return isValidatePass;
        }
        #endregion
    }
}
