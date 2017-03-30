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
    public partial class RichItemsControl : UserControl
    {
        private IEnumerable<RichItemViewModel> _itemsSource;
        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                (Resources["ImageConverter"] as ImagePathConverter).ImageDirectory = value;
                _imagePath = value;
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
                _view.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));
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
            RichItemViewModel vm = item as RichItemViewModel;
            if (vm is TextBoxItemViewModel)
            {
                return TextBoxDataTemplate;
            }
            else if (vm is ImageComboBoxItemViewModel)
            {
                return ImageComboBoxDataTemplate;
            }
            else if (vm is StringComboBoxItemViewModel)
            {
                switch (((StringComboBoxItemViewModel)vm).Type)
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
        private string _groupName;
        private string _longName;
        private object _value;

        #region 构造函数
        public RichItemViewModel(string groupName, string longName, object value)
        {
            _groupName = groupName;
            _longName = longName;
            _value = value;
        }
        #endregion

        #region 属性
        public string GroupName
        {
            get { return _groupName; }
        }

        public string LongName
        {
            get { return _longName; }
        }

        public virtual object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
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

    public class TextBoxItemViewModel : RichItemViewModel, INotifyDataErrorInfo
    {
        const string INT_ERROR_TIP = "请输入整数！";
        const string DOUBLE_ERROR_TIP = "请输入数值！";
        const string STRING_ERROR_TIP = "请输入文字！";
        private string _errorDesription;
        private bool _hasErrors;

        #region 属性
        public override object Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                _hasErrors = false;
                if (_errorDesription == INT_ERROR_TIP)
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

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Value"));
                base.Value = value;
            }
        }
        #endregion

        public TextBoxItemViewModel(string groupName, string longName, object value) : base(groupName, longName, value)
        {
            Type valueType = value.GetType();
            if (valueType == typeof(int))
                _errorDesription = INT_ERROR_TIP;
            else if (valueType == typeof(double))
                _errorDesription = DOUBLE_ERROR_TIP;
            else if (valueType == typeof(string))
                _errorDesription = STRING_ERROR_TIP;
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

    public class StringComboBoxItemViewModel : RichItemViewModel
    {
        private IEnumerable _itemsSource;
        private StringComboBoxType _type;

        public IEnumerable ItemsSource
        {
            get { return _itemsSource; }
        }

        public StringComboBoxType Type
        {
            get { return _type; }
        }

        public StringComboBoxItemViewModel(string groupName, string longName, object value, IEnumerable<string> itemsSource, StringComboBoxType type) : base(groupName, longName, value)
        {
            _itemsSource = itemsSource;
            _type = type;
        }
    }

    public class ImageComboBoxItemViewModel : RichItemViewModel
    {
        private IEnumerable _itemsSource;

        public IEnumerable ItemsSource
        {
            get { return _itemsSource; }
        }

        public ImageComboBoxItemViewModel(string groupName, string longName, int id, IEnumerable<ImageAttribute> itemsSource) : base(groupName, longName, GetValueById(itemsSource, id))
        {
            _itemsSource = itemsSource;
        }

        private static object GetValueById(IEnumerable<ImageAttribute> itemsSource, int id)
        {
            foreach (var item in itemsSource)
            {
                if (item.Id == id) return item;
            }
            return null;
        }

        public class ImageAttribute
        {
            public int Id { get; }
            public string Name { get; }
            public string Description { get; }
            public string Path { get; }

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
        }
    }
    #endregion

    #region 值转换器
    public class ImagePathConverter : IValueConverter
    {
        public string ImageDirectory { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = Path.Combine(ImageDirectory, (string)value);
            return new BitmapImage(new Uri(imagePath));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
