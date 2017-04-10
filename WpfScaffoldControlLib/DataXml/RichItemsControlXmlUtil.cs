using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using XcWpfControlLib.Control;

namespace XcWpfControlLib.DataXml
{
    public static class RichItemsControlXmlUtil
    {
        public static void Write(IEnumerable<RichItemViewModel> viewModels, string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Configure");

            Dictionary<string, int> groupStringHashTable = new Dictionary<string, int>();
            int count = 1;
            foreach (var viewModel in viewModels)
            {
                if (!groupStringHashTable.ContainsKey(viewModel.Group))
                {
                    groupStringHashTable.Add(viewModel.Group, count++);
                }
            }
            XmlElement groupTable = doc.CreateElement("GroupTable");
            foreach (var item in groupStringHashTable)
            {
                XmlElement group = doc.CreateElement("Group");
                group.SetAttribute("Key", item.Key);
                group.SetAttribute("Value", item.Value.ToString());
                groupTable.AppendChild(group);
            }
            root.AppendChild(groupTable);

            RichItemIOHelper io = new RichItemIOHelper();
            foreach (var viewModel in viewModels)
            {
                io.Write(root, viewModel, groupStringHashTable);
            }
            doc.AppendChild(root);
            doc.Save(path);
        }

        public static void Read(RichItemsControl control, string path)
        {
            control.ImageDir = Path.Combine(Path.GetDirectoryName(path), "Image");
            control.ItemsSource = Read(path);
        }

        public static ObservableCollection<RichItemViewModel> Read(string path)
        {
            XElement doc = XElement.Load(path);

            Dictionary<int, string> groupNumHashTable = new Dictionary<int, string>();
            foreach (var element in doc.Element("GroupTable").Elements())
            {
                groupNumHashTable.Add(int.Parse(element.Attribute("Value").Value), element.Attribute("Key").Value);
            }

            ObservableCollection<RichItemViewModel> itemsSource = new ObservableCollection<RichItemViewModel>();
            RichItemIOHelper io = new RichItemIOHelper();
            foreach (var element in doc.Elements())
            {
                if (element.Name != "GroupTable")
                {
                    itemsSource.Add(io.Read(element, groupNumHashTable));
                }
            }
            return itemsSource;
        }
    }

    class RichItemIOHelper
    {
        private RichItemXml _current = TextBoxItemXml.Create();

        public void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            _current = _current.GetSuitedWriter(viewModel);
            _current.Write(root, viewModel, groupStringHashTable);
        }

        public RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            _current = _current.GetSuitedReader(item);
            return _current.Read(item, groupNumHashTable);
        }
    }

    #region 数据模型读写
    abstract class RichItemXml
    {
        public abstract RichItemXml GetSuitedWriter(RichItemViewModel viewModel);
        public abstract RichItemXml GetSuitedReader(XElement item);
        public abstract void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable);
        public abstract RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable);
    }

    /// <summary>
    /// 文本框数据模型读写
    /// </summary>
    class TextBoxItemXml : RichItemXml
    {
        private static TextBoxItemXml _singleton = new TextBoxItemXml();

        private TextBoxItemXml() { }

        public static TextBoxItemXml Create()
        {
            return _singleton;
        }

        public override RichItemXml GetSuitedReader(XElement item)
        {
            if (item.Name == "TextBoxItem")
            {
                return this;
            }
            return StringComboBoxItemXml.Create().GetSuitedReader(item);
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is TextBoxItemViewModel)
            {
                return this;
            }
            return StringComboBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            string group = groupNumHashTable[int.Parse(item.Attribute("Group").Value)];
            string name = item.Attribute("Key").Value;
            string value = item.Attribute("Value").Value;
            return new TextBoxItemViewModel(group, name, value);
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            TextBoxItemViewModel textBoxVM = (TextBoxItemViewModel)viewModel;
            XmlElement textBoxItem = root.OwnerDocument.CreateElement("TextBoxItem");
            textBoxItem.SetAttribute("Group", groupStringHashTable[textBoxVM.Group].ToString());
            textBoxItem.SetAttribute("Key", textBoxVM.Name);
            textBoxItem.SetAttribute("Value", textBoxVM.Value != null ? textBoxVM.Value.ToString() : "");
            root.AppendChild(textBoxItem);
        }
    }

    /// <summary>
    /// 文本下拉框数据模型读写
    /// </summary>
    class StringComboBoxItemXml : RichItemXml
    {
        private static StringComboBoxItemXml _singleton = new StringComboBoxItemXml();

        private StringComboBoxItemXml() { }

        public static StringComboBoxItemXml Create()
        {
            return _singleton;
        }

        public override RichItemXml GetSuitedReader(XElement item)
        {
            if (item.Name == "SingleComboBoxItem" || item.Name == "MultiComboBoxItem")
            {
                return this;
            }
            return ImageComboBoxItemXml.Create().GetSuitedReader(item);
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is StringComboBoxItemViewModel)
            {
                return this;
            }
            return ImageComboBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            string group = groupNumHashTable[int.Parse(item.Attribute("Group").Value)];
            string name = item.Attribute("Key").Value;
            string value = item.Attribute("Value").Value;
            string[] itemsSource = item.Attribute("Items").Value.Split(';');
            StringComboBoxType type = item.Name == "SingleComboBoxItem" ?
                StringComboBoxType.Single :
                StringComboBoxType.Multiple;
            return new StringComboBoxItemViewModel(group, name, value, itemsSource, type);
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            StringComboBoxItemViewModel stringComboBoxVM = (StringComboBoxItemViewModel)viewModel;
            XmlElement comboBoxItem = stringComboBoxVM.StringType == StringComboBoxType.Single ?
                root.OwnerDocument.CreateElement("SingleComboBoxItem") :
                root.OwnerDocument.CreateElement("MultiComboBoxItem");
            comboBoxItem.SetAttribute("Group", groupStringHashTable[stringComboBoxVM.Group].ToString());
            comboBoxItem.SetAttribute("Key", stringComboBoxVM.Name);
            comboBoxItem.SetAttribute("Value", stringComboBoxVM.Value != null ? stringComboBoxVM.Value.ToString() : "");
            SetItemsAttribute(comboBoxItem, stringComboBoxVM.StringsSource);
            root.AppendChild(comboBoxItem);
        }

        private void SetItemsAttribute(XmlElement root, IEnumerable<string> itemsSource)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in itemsSource)
            {
                sb.Append(item + ";");
            }
            root.SetAttribute("Items", sb.ToString().TrimEnd(';'));
        }
    }

    /// <summary>
    /// 图片下拉框数据模型读写
    /// </summary>
    class ImageComboBoxItemXml : RichItemXml
    {
        private static ImageComboBoxItemXml _singleton = new ImageComboBoxItemXml();

        private ImageComboBoxItemXml() { }

        public static ImageComboBoxItemXml Create()
        {
            return _singleton;
        }

        public override RichItemXml GetSuitedReader(XElement item)
        {
            if (item.Name == "ImageComboBoxItem")
            {
                return this;
            }
            return TextBoxItemXml.Create().GetSuitedReader(item);
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is ImageComboBoxItemViewModel)
            {
                return this;
            }
            return TextBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            string group = groupNumHashTable[int.Parse(item.Attribute("Group").Value)];
            string name = item.Attribute("Key").Value;
            int id = int.Parse(item.Attribute("Value").Value);
            IEnumerable<ImageComboBoxItemViewModel.ImageAttribute> itemsSource = ReadItemsSource(item);
            return new ImageComboBoxItemViewModel(group, name, id, itemsSource);
        }

        private IEnumerable<ImageComboBoxItemViewModel.ImageAttribute> ReadItemsSource(XElement root)
        {
            List<ImageComboBoxItemViewModel.ImageAttribute> result = new List<ImageComboBoxItemViewModel.ImageAttribute>(16);
            foreach (var item in root.Elements())
            {
                int id = int.Parse(item.Attribute("Id").Value);
                string name = item.Attribute("Name").Value;
                string description = item.Attribute("Description").Value;
                string path = item.Attribute("Path").Value;
                result.Add(new ImageComboBoxItemViewModel.ImageAttribute(id, name, description, path));
            }
            return result;
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            ImageComboBoxItemViewModel imageComboBoxVM = (ImageComboBoxItemViewModel)viewModel;
            XmlElement imageComboBoxItem = root.OwnerDocument.CreateElement("ImageComboBoxItem");
            imageComboBoxItem.SetAttribute("Group", groupStringHashTable[imageComboBoxVM.Group].ToString());
            imageComboBoxItem.SetAttribute("Key", imageComboBoxVM.Name);
            imageComboBoxItem.SetAttribute("Value", imageComboBoxVM.Value != null ? imageComboBoxVM.Value.ToString() : "");
            SetItemsChildrens(imageComboBoxItem, imageComboBoxVM.ImagesSource);
            root.AppendChild(imageComboBoxItem);
        }

        private void SetItemsChildrens(XmlElement root, IEnumerable<ImageComboBoxItemViewModel.ImageAttribute> itemsSource)
        {
            foreach (var item in itemsSource)
            {
                XmlElement itemElement = root.OwnerDocument.CreateElement("Item");
                itemElement.SetAttribute("Id", item.Id.ToString());
                itemElement.SetAttribute("Name", item.Name);
                itemElement.SetAttribute("Description", item.Description);
                itemElement.SetAttribute("Path", item.Path);
                root.AppendChild(itemElement);
            }
        }
    }
    #endregion
}
