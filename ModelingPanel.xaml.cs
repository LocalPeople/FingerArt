using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace XcWpfControlLib.WpfScaffoldControlLib
{
    /// <summary>
    /// ModelingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ModelingPanel : UserControl
    {
        public List<ModelingViewModel> ViewModel { get; private set; }

        public ModelingPanel()
        {
            InitializeComponent();
        }

        public void SetViewModel(List<string> notYetBuilt, List<string> alreadyBuilt)
        {
            ViewModel = new List<ModelingViewModel>();

            foreach (var item in notYetBuilt)
                ViewModel.Add(new ModelingViewModel() { Level = item, State = false, StateText = "×" });
            foreach (var item in alreadyBuilt)
                ViewModel.Add(new ModelingViewModel() { Level = item, State = true, StateText = "√" });
        }

        public void GetModelingChanged(out List<string> add, out List<string> delete)
        {
            add = new List<string>();
            delete = new List<string>();

            foreach (var item in ViewModel)
            {
                if (item.State && item.StateText == "×")
                    add.Add(item.Level);
                else if (!item.State && item.StateText == "√")
                    delete.Add(item.Level);
            }
        }

        #region 勾选框相关事件
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int index = ViewModel.IndexOf(ViewModel.First(vm => vm.State));

            if (index == ViewModel.Count - 1 || ViewModel[index + 1].State)
                return;

            for (; index < ViewModel.Count; index++)
                ViewModel[index].State = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int index = ViewModel.IndexOf(ViewModel.Last(vm => !vm.State));

            if (index == 0 || !ViewModel[index - 1].State)
                return;

            for (; index >= 0; index--)
                ViewModel[index].State = false;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            CheckBox checkBox = (CheckBox)textBlock.Tag;
            checkBox.IsChecked = !checkBox.IsChecked;
        }
        #endregion
    }

    #region 数据模板选择器
    //public class ModelingItemTemplateSelector : DataTemplateSelector
    //{
    //    public DataTemplate UnbuiltDataTemplate { get; set; }
    //    public DataTemplate BuiltDataTemplate { get; set; }

    //    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    //    {
    //        ModelingViewModel vm = item as ModelingViewModel;
    //        if (vm.StateText == "×")
    //            return UnbuiltDataTemplate;
    //        else
    //            return BuiltDataTemplate;
    //    }
    //}
    #endregion

    #region 模型生成数据模型
    public class ModelingViewModel : INotifyPropertyChanged
    {
        private bool _state;

        public string Level { get; internal set; }
        public bool State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }
        public string StateText { get; internal set; }

        #region 事件
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is ModelingViewModel)) return false;
            return Level.Equals((obj as ModelingViewModel).Level);
        }

        public override int GetHashCode()
        {
            return Level.GetHashCode();
        }
    }
    #endregion
}
