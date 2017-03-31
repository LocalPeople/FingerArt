using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using XcWpfControlLib.Control;

namespace XcWpfControlLib.DataXml
{
    public static class RichItemsControlUtil
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
                group.SetAttribute("Name", item.Key);
                group.SetAttribute("Number", item.Value.ToString());
                groupTable.AppendChild(group);
            }

            RichItemIOHelper io = new RichItemIOHelper();
            foreach (var viewModel in viewModels)
            {
                io.Write(root, viewModel, groupStringHashTable);
            }
            doc.AppendChild(root);
            doc.Save(path);
        }

        public static IEnumerable<RichItemViewModel> Read(string path)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is TextBoxItemViewModel)
            {
                return _singleton;
            }
            return StringComboBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            throw new NotImplementedException();
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            TextBoxItemViewModel textBoxVM = (TextBoxItemViewModel)viewModel;
            XmlElement textBoxItem = root.OwnerDocument.CreateElement("TextBoxItem");
            textBoxItem.SetAttribute("Group", groupStringHashTable[textBoxVM.Group].ToString());
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
            throw new NotImplementedException();
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is StringComboBoxItemViewModel)
            {
                return _singleton;
            }
            return ImageComboBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            throw new NotImplementedException();
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override RichItemXml GetSuitedWriter(RichItemViewModel viewModel)
        {
            if (viewModel is ImageComboBoxItemViewModel)
            {
                return _singleton;
            }
            return TextBoxItemXml.Create().GetSuitedWriter(viewModel);
        }

        public override RichItemViewModel Read(XElement item, Dictionary<int, string> groupNumHashTable)
        {
            throw new NotImplementedException();
        }

        public override void Write(XmlElement root, RichItemViewModel viewModel, Dictionary<string, int> groupStringHashTable)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
