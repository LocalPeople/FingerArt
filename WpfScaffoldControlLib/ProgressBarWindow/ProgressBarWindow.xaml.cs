using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace XcWpfControlLib
{
    /// <summary>
    /// ProgressBarWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarWindow : Window
    {
        public static ProgressArgs ProgressShow()
        {
            ProgressArgs args = new ProgressArgs();
            Thread th = new Thread(new ParameterizedThreadStart(InitializeShow));
            th.SetApartmentState(ApartmentState.STA);
            th.IsBackground = true;
            th.Start(args);
            return args;
        }

        private static void InitializeShow(object obj)
        {
            ProgressArgs args = obj as ProgressArgs;
            ProgressBarWindow pbw = new ProgressBarWindow(args);
            pbw.DataContext = args;
            pbw.Show();
            Dispatcher.Run();
        }

        internal ProgressBarWindow()
        {
            InitializeComponent();
        }

        private DispatcherTimer _timer;
        private ProgressArgs _args;
        public ProgressBarWindow(ProgressArgs args)
        {
            InitializeComponent();

            _args = args;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_args != null)
            {
                if (_args.IsClosed)
                {
                    _timer.Stop();
                    _timer = null;
                    Close();
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _args.Cancel();
            _args.Text = "正在取消，请稍等……";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_args.IsClosed)
            {
                _timer.Stop();
                _timer = null;
                _args.Cancel();
                _args.Close();
            }
        }
    }

    public class ProgressArgs : INotifyPropertyChanged
    {
        #region 私有字段
        private string _title = "脚手架智能设计软件";
        private string _text = "请稍等……";
        private string _tip;
        private double _maximum = 1;
        private double _value = 0;
        private bool _isIndeterminate = false;
        private bool _isClosed = false;
        private bool _isCancel = false;
        private bool _canCancel = true;
        #endregion

        internal ProgressArgs()
        {

        }

        public void Change()
        {
            if (_isCancel)
            {
                _isClosed = true;
                throw new Exception("用户取消");
            }
            if (!_isIndeterminate)
            {
                Value++;
                Text = string.Format("{0} {1}%", Tip, (int)((100 * _value) / _maximum));
            }
        }

        public void Close()
        {
            _isClosed = true;
        }

        public void Cancel()
        {
            _isCancel = true;
        }

        #region 属性
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string Tip
        {
            get { return _tip; }
            set
            {
                _tip = value;
                Text = string.Format("{0} {1}%", _tip, 0);
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public double Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                OnPropertyChanged("Maximum");
            }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set
            {
                _isIndeterminate = value;
                OnPropertyChanged("IsIndeterminate");
            }
        }

        public bool IsClosed { get { return _isClosed; } }

        public bool IsCancel { get { return _isCancel; } }

        public bool CanCancel
        {
            get { return _canCancel; }
            set
            {
                _canCancel = value;
                OnPropertyChanged("CanCancel");
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
