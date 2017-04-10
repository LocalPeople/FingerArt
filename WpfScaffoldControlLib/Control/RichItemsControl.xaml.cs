using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace XcWpfControlLib.Control
{
    /// <summary>
    /// RichItemsControl.xaml 的交互逻辑
    /// </summary>
    public partial class RichItemsControl : UserControl, INotifyPropertyChanged
    {
        private IEnumerable<RichItemViewModel> _itemsSource;
        private string _imagePath;
        public string ImageDir
        {
            get { return _imagePath; }
            set
            {
                (Resources["ImageConverter"] as ImagePathConverter).ImageDirectory = value;
                _imagePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageDir"));
            }
        }

        public RichItemsControl()
        {
            InitializeComponent();
        }

        #region 属性成员
        public IEnumerable<RichItemViewModel> ItemsSource
        {
            get
            {
                return _itemsSource;
            }
            set
            {
                _itemsSource = value;
                ListCollectionView _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_itemsSource);
                _view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                _view.IsLiveGrouping = true;// 启动分组实时更新
                _view.LiveGroupingProperties.Add("Group");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ItemsSource"));
            }
        }
        #endregion

        #region 事件
        private int _errorCount = 0;
        private void itemsControl1_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added) _errorCount++;
            else _errorCount--;
            OnErrorsChanged?.Invoke(this, _errorCount);
        }

        public event EventHandler<int> OnErrorsChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    #region 数据模板选择器
    public class RichItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBoxDataTemplate { get; set; }
        public DataTemplate ComboBoxDataTemplate { get; set; }
        public DataTemplate ImageComboBoxDataTemplate { get; set; }
        public DataTemplate MultipleComboBoxDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TextBoxItemViewModel)
            {
                return TextBoxDataTemplate;
            }
            else if (item is ImageComboBoxItemViewModel)
            {
                return ImageComboBoxDataTemplate;
            }
            else if (item is StringComboBoxItemViewModel)
            {
                switch (((StringComboBoxItemViewModel)item).StringType)
                {
                    case StringComboBoxType.Single:
                        return ComboBoxDataTemplate;
                    case StringComboBoxType.Multiple:
                        return MultipleComboBoxDataTemplate;
                }
            }
            return default(DataTemplate);
        }
    }
    #endregion

    #region 数据模型
    public abstract class RichItemViewModel : INotifyPropertyChanged
    {
        private string _group;
        private string _name;
        private object _value;
        private RichItemType _type;

        #region 构造函数
        public RichItemViewModel(string group, string name, object value)
        {
            _group = group;
            _name = name;
            _value = value;
        }

        public RichItemViewModel(string group, string name, object value, RichItemType type) : this(group, name, value)
        {
            _type = type;
        }
        #endregion

        #region 属性
        public string Group
        {
            get { return _group; }
            set
            {
                _group = value;
                OnPropertyChanged("Group");
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public RichItemType Type
        {
            get { return _type; }
            set
            {
                _type = value;
                OnPropertyChanged("Type");
            }
        }
        #endregion

        #region INotifyPropertyChanged
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    public abstract class RichItemViewModel<T> : RichItemViewModel
    {
        public RichItemViewModel(string group, string name, T value, RichItemType type) : base(group, name, value, type)
        {
        }

        public RichItemViewModel(string group, string name, T value) : base(group, name, value)
        {
        }

        public virtual new T Value
        {
            get
            {
                return (T)base.Value;
            }
            set
            {
                base.Value = value;
            }
        }
    }


    public enum RichItemType
    {
        文本输入框,
        整数输入框,
        小数输入框,
        文本下拉单选框,
        文本下拉多选框,
        图片下拉单选框,
    }

    public class TextBoxItemViewModel : RichItemViewModel<string>, INotifyDataErrorInfo
    {
        const string INT_ERROR_TIP = "请输入整数！";
        const string DOUBLE_ERROR_TIP = "请输入数值！";
        const string STRING_ERROR_TIP = "请输入文字！";
        private string _errorDesription;
        private bool _hasErrors;

        #region 属性
        public override string Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                _hasErrors = false;
                if (value == null)
                {
                    goto _SkipValidation;
                }
                else if (_errorDesription == INT_ERROR_TIP)
                {
                    int result;
                    if (!int.TryParse(value.ToString(), out result))
                    {
                        _hasErrors = true;
                    }
                }
                else if (_errorDesription == DOUBLE_ERROR_TIP)
                {
                    double result;
                    if (!double.TryParse(value.ToString(), out result))
                    {
                        _hasErrors = true;
                    }
                }
                else if (_errorDesription == STRING_ERROR_TIP)
                {
                    double result;
                    if (double.TryParse(value.ToString(), out result))
                    {
                        _hasErrors = true;
                    }
                }

                _SkipValidation:
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Value"));
                base.Value = value;
            }
        }
        #endregion

        public TextBoxItemViewModel(string group, string name, string value) : base(group, name, value)
        {
            string valueToString = value.ToString();
            int intTemp;
            double doubleTemp;
            if (int.TryParse(valueToString, out intTemp))
            {
                _errorDesription = INT_ERROR_TIP;
                Type = RichItemType.整数输入框;
            }
            else if (double.TryParse(valueToString, out doubleTemp))
            {
                _errorDesription = DOUBLE_ERROR_TIP;
                Type = RichItemType.小数输入框;
            }
            else
            {
                _errorDesription = STRING_ERROR_TIP;
                Type = RichItemType.文本输入框;
            }
        }

        #region INotifyDataErrorInfo
        public bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            yield return _errorDesription;
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }

    public enum StringComboBoxType
    {
        Single,
        Multiple,
    }

    public class StringComboBoxItemViewModel : RichItemViewModel<string>
    {
        private IEnumerable<string> _itemsSource;
        private StringComboBoxType _type;

        public IEnumerable<string> StringsSource
        {
            get { return _itemsSource; }
            set
            {
                _itemsSource = value;
                OnPropertyChanged("StringsSource");
            }
        }

        public StringComboBoxType StringType
        {
            get { return _type; }
        }

        public StringComboBoxItemViewModel(string group, string name, string value, IEnumerable<string> itemsSource, StringComboBoxType type) : base(group, name, value, GetRichItemType(type))
        {
            _itemsSource = itemsSource;
            _type = type;
        }

        private static RichItemType GetRichItemType(StringComboBoxType type)
        {
            return type == StringComboBoxType.Single ? RichItemType.文本下拉单选框 : RichItemType.文本下拉多选框;
        }
    }

    public class ImageComboBoxItemViewModel : RichItemViewModel<ImageComboBoxItemViewModel.ImageAttribute>
    {
        private IEnumerable<ImageAttribute> _itemsSource;

        public IEnumerable<ImageAttribute> ImagesSource
        {
            get { return _itemsSource; }
            set
            {
                _itemsSource = value;
                OnPropertyChanged("ImagesSource");
            }
        }

        public ImageComboBoxItemViewModel(string group, string name, int id, IEnumerable<ImageAttribute> itemsSource) : base(group, name, GetValueById(itemsSource, id), RichItemType.图片下拉单选框)
        {
            _itemsSource = itemsSource;
        }

        private static ImageAttribute GetValueById(IEnumerable<ImageAttribute> itemsSource, int id)
        {
            if (itemsSource != null)
            {
                foreach (var item in itemsSource)
                {
                    if (item.Id == id) return item;
                }
            }
            return new ImageAttribute();
        }

        public class ImageAttribute
        {
            public int Id { get; }
            public string Name { get; }
            public string Description { get; }
            public string Path { get; }

            public ImageAttribute()
            {
                Id = -1;
                Name = string.Empty;
                Description = string.Empty;
                Path = string.Empty;
            }

            public ImageAttribute(int id, string name, string description, string path)
            {
                Id = id;
                Name = name;
                Description = description;
                Path = path;
            }

            public override string ToString()
            {
                return Id.ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj is ImageAttribute)
                {
                    return Path.Equals(((ImageAttribute)obj).Path);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Path.GetHashCode();
            }
        }
    }
    #endregion

    #region 值转换器
    public class ImagePathConverter : IValueConverter
    {
        public string ImageDirectory { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = string.IsNullOrWhiteSpace(ImageDirectory) ?
                value.ToString() :
                Path.Combine(ImageDirectory, (string)value);
            return File.Exists(imagePath) ? new BitmapImage(new Uri(imagePath)) : new BitmapImage(new Uri(@"../Resources/img-error.jpg", UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
