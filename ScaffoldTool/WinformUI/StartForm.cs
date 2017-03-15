using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ScaffoldTool
{
    /// <summary>
    /// 参数设置界面类
    /// </summary>
    internal partial class StartForm : Form
    {
        public string[] Keys { get; private set; }
        public string[] Values { get; private set; }
        private double DSGD;
        private FileInfo[] _configFileInfos;
        private string assemblyPath = Assembly.GetExecutingAssembly().Location;
        private string _appConfigPath = Assembly.GetExecutingAssembly().Location + ".config";

        public StartForm(XC_GenericModel gm)
        {
            InitializeComponent();
            DSGD = gm.LevelSet.Height * 0.3048;
            Keys = new string[40];// 输入参数数量
            Values = new string[40];
        }

        public StartForm()
        {
            InitializeComponent();
        }

        private bool isFirstLoad = false;// true将开启动画效果

        private void StartForm_Load(object sender, EventArgs e)
        {
            if (isFirstLoad)
            {
                foreach (Control control in this.Controls)
                    control.Visible = false;
                this.Size = new System.Drawing.Size(422, 20);
                StoryBoard.StoryBoard storyBoard = new StoryBoard.StoryBoard(this, StartForm_RealLoad);
                StoryBoard.SizeAnimation sizeAnimation = new StoryBoard.SizeAnimation(this, "Size", 500, this.Size, new System.Drawing.Size(422, 468));
                storyBoard.Add(sizeAnimation);
                storyBoard.Start(400);
            }
            else
                StartForm_RealLoad();
        }

        private void StartForm_RealLoad()
        {
            DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(assemblyPath));// 获取当前运行程序集工作目录
            this.XMDZ.Items.Clear();// 清除上次保存的下拉项
            _configFileInfos = dir.GetFiles(Path.GetFileName(assemblyPath + ".*.config"));// 获取本地配置文件
            if (_configFileInfos.Length > 0)
            {
                foreach (var configFileInfo in _configFileInfos)
                {
                    this.XMDZ.Items.Add(configFileInfo.Name.Split('.')[2]);
                }
            }
            //XElement appConfigXElement = XElement.Load(_appConfigPath);
            //string selectedItem = appConfigXElement.Element("appSettings").Elements().First().Attribute("value").Value;
            //if (!string.IsNullOrEmpty(selectedItem))
            //    this.XMDZ.SelectedItem = appConfigXElement.Element("appSettings").Elements().First().Attribute("value").Value;
            foreach (Control control in this.Controls)
            {
                if (control.Name == "label28") continue;
                control.Visible = true;
            }
            isFirstLoad = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.errorProvider1.Clear();
            if (string.IsNullOrWhiteSpace(XMDZ.Text))
            {
                this.errorProvider1.SetError(XMDZ, "请输入项目所在地区");
                return;
            }
            Global.LGZJ = double.Parse(LGZJ.Text) / 0.3048;// m to inch
            Global.LGHJ = double.Parse(LGHJ.Text) / 0.3048;// m to inch
            Global.BJ = double.Parse(BJ.Text) / 0.3048;// m to inch
            Global.LQJKS = int.Parse(LQJKS.Text);
            Global.NJJQ = double.Parse(NJJQ.Text) / 304.8;// mm to inch
            Global.BOTTOM_PLATE_LENGTH = Math.Sqrt(double.Parse(JCMJ.Text)) / 0.3048;// m to inch
            int index = 0;
            foreach (TextBox textBox in this.panel1.Controls.OfType<TextBox>())
            {
                if (textBox.Visible == false) continue;
                Keys[index] = textBox.Name.TrimStart('_');
                Values[index] = textBox.Text;
                index++;
            }
            Keys[index] = "DSGD";
            Values[index++] = Math.Round(DSGD, 3).ToString();
            Keys[index] = "XMDZ";
            Values[index] = XMDZ.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.errorProvider1.Clear();
            if (string.IsNullOrWhiteSpace(XMDZ.Text))
            {
                this.errorProvider1.SetError(XMDZ, "请输入项目所在地区");
            }
            else
            {
                string copyPath = string.Format("{0}.{1}.config", Assembly.GetExecutingAssembly().Location, XMDZ.Text);
                XDocument xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("configuration",
                        new XElement("appSettings",
                            new XElement("add", new XAttribute("key", "立杆纵距"), new XAttribute("value", LGZJ.Text)),
                            new XElement("add", new XAttribute("key", "立杆横距"), new XAttribute("value", LGHJ.Text)),
                            new XElement("add", new XAttribute("key", "步距"), new XAttribute("value", BJ.Text)),
                            new XElement("add", new XAttribute("key", "连墙件步数"), new XAttribute("value", LQJBS.Text)),
                            new XElement("add", new XAttribute("key", "连墙件跨数"), new XAttribute("value", LQJKS.Text)),
                            new XElement("add", new XAttribute("key", "内排架距墙长度"), new XAttribute("value", NJJQ.Text)),
                            new XElement("add", new XAttribute("key", "扣件抗滑承载力系数"), new XAttribute("value", ZJXS.Text)),
                            new XElement("add", new XAttribute("key", "脚手架用途"), new XAttribute("value", JSJYT.Text)),
                            new XElement("add", new XAttribute("key", "施工荷载均布参数"), new XAttribute("value", SGHZ.Text)),
                            new XElement("add", new XAttribute("key", "同时施工层数"), new XAttribute("value", SGCS.Text)),
                            new XElement("add", new XAttribute("key", "基本风压"), new XAttribute("value", JBFY.Text)),
                            new XElement("add", new XAttribute("key", "立杆稳定性时风压高度变化系数"), new XAttribute("value", LGFY.Text)),
                            new XElement("add", new XAttribute("key", "连墙件强度时风压高度变化系数"), new XAttribute("value", LQJFY.Text)),
                            new XElement("add", new XAttribute("key", "风荷载体型系数"), new XAttribute("value", TXXS.Text)),
                            new XElement("add", new XAttribute("key", "每米立杆数承受的结构自重"), new XAttribute("value", LGZZ.Text)),
                            new XElement("add", new XAttribute("key", "脚手板类别"), new XAttribute("value", JSBLB.Text)),
                            new XElement("add", new XAttribute("key", "脚手板自重标准值"), new XAttribute("value", JSBZZ.Text)),
                            new XElement("add", new XAttribute("key", "挡板类别"), new XAttribute("value", DJBLB.Text)),
                            new XElement("add", new XAttribute("key", "挡脚板自重标准值"), new XAttribute("value", DJBZZ.Text)),
                            new XElement("add", new XAttribute("key", "安全设施与安全网自重标准值"), new XAttribute("value", AQWZZ.Text)),
                            new XElement("add", new XAttribute("key", "地基土类型"), new XAttribute("value", DJTLX.Text)),
                            new XElement("add", new XAttribute("key", "地基承载力标准值"), new XAttribute("value", DJCZL.Text)),
                            new XElement("add", new XAttribute("key", "基础底面扩展面积"), new XAttribute("value", JCMJ.Text)),
                            new XElement("add", new XAttribute("key", "基础降低系数"), new XAttribute("value", JCXS.Text)),
                            new XElement("add", new XAttribute("key", "计算长度系数"), new XAttribute("value", JSCDXS.Text)),
                            new XElement("add", new XAttribute("key", "轴心受压立杆的稳定系数"), new XAttribute("value", LGWDXS.Text)),
                            new XElement("add", new XAttribute("key", "该分段顶部高度"), new XAttribute("value", _1DSGD.Text)),
                            new XElement("add", new XAttribute("key", "型钢型号"), new XAttribute("value", XGXH.Text)),
                            new XElement("add", new XAttribute("key", "楼板混凝土标号"), new XAttribute("value", HNTBH.Text)),
                            new XElement("add", new XAttribute("key", "型钢截面面积"), new XAttribute("value", JMMJ.Text)),
                            new XElement("add", new XAttribute("key", "型钢理论质量"), new XAttribute("value", LLZL.Text)),
                            new XElement("add", new XAttribute("key", "转动半径"), new XAttribute("value", ZDBJ.Text)),
                            new XElement("add", new XAttribute("key", "截面模量"), new XAttribute("value", JMML.Text)),
                            new XElement("add", new XAttribute("key", "转动惯量"), new XAttribute("value", ZDGL.Text)),
                            new XElement("add", new XAttribute("key", "混凝土抗拉强度"), new XAttribute("value", HNTKLQD.Text)),
                            new XElement("add", new XAttribute("key", "混凝土抗压强度"), new XAttribute("value", HNTKYQD.Text)),
                            new XElement("add", new XAttribute("key", "锚固段长度与悬挑段长度比值"), new XAttribute("value", MGXTBZ.Text)),
                            new XElement("add", new XAttribute("key", "本段顶部风压高度变化系数"), new XAttribute("value", _1LQJFY.Text)))));
                xDoc.Save(copyPath);
                XElement xElement = XElement.Load(_appConfigPath);
                xElement.Element("appSettings").Elements().First().Attribute("value").SetValue(this.XMDZ.Text);
                xElement.Save(_appConfigPath);
                this.label28.Visible = true;// 保存提示
            }
        }

        private void OnlyDouble_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(e.KeyChar >= 48 && e.KeyChar <= 57) && e.KeyChar != 8 && e.KeyChar != '.')
                e.Handled = true;
            if (e.KeyChar == '.' && (string.IsNullOrWhiteSpace(((TextBox)sender).Text) || ((TextBox)sender).Text.IndexOf('.') != -1))
                e.Handled = true;
            if (e.KeyChar == '0' && ((TextBox)sender).Text == "0")
                e.Handled = true;
        }

        private void OnlyInt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(e.KeyChar >= 48 && e.KeyChar <= 57) && e.KeyChar != 8)
                e.Handled = true;
            if (e.KeyChar == '0' && ((TextBox)sender).Text == "0")
                e.Handled = true;
        }

        /// <summary>
        /// 项目地区更改时载入该地区本地配置文件数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XMDZ_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Configuration.ExeConfigurationFileMap configFile = new System.Configuration.ExeConfigurationFileMap();
            configFile.ExeConfigFilename = _configFileInfos[this.XMDZ.SelectedIndex].FullName;
            System.Configuration.Configuration cfa =
                System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configFile, System.Configuration.ConfigurationUserLevel.None);
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            LGZJ.Text = cfa.AppSettings.Settings["立杆纵距"].Value;
            LGHJ.Text = cfa.AppSettings.Settings["立杆横距"].Value;
            BJ.Text = cfa.AppSettings.Settings["步距"].Value;
            LQJBS.Text = cfa.AppSettings.Settings["连墙件步数"].Value;
            LQJKS.Text = cfa.AppSettings.Settings["连墙件跨数"].Value;
            NJJQ.Text = cfa.AppSettings.Settings["内排架距墙长度"].Value;
            ZJXS.Text = cfa.AppSettings.Settings["扣件抗滑承载力系数"].Value;
            JSJYT.Text = cfa.AppSettings.Settings["脚手架用途"].Value;
            SGHZ.Text = cfa.AppSettings.Settings["施工荷载均布参数"].Value;
            SGCS.Text = cfa.AppSettings.Settings["同时施工层数"].Value;
            JBFY.Text = cfa.AppSettings.Settings["基本风压"].Value;
            LGFY.Text = cfa.AppSettings.Settings["立杆稳定性时风压高度变化系数"].Value;
            LQJFY.Text = cfa.AppSettings.Settings["连墙件强度时风压高度变化系数"].Value;
            TXXS.Text = cfa.AppSettings.Settings["风荷载体型系数"].Value;
            LGZZ.Text = cfa.AppSettings.Settings["每米立杆数承受的结构自重"].Value;
            JSBLB.Text = cfa.AppSettings.Settings["脚手板类别"].Value;
            JSBZZ.Text = cfa.AppSettings.Settings["脚手板自重标准值"].Value;
            DJBLB.Text = cfa.AppSettings.Settings["挡板类别"].Value;
            DJBZZ.Text = cfa.AppSettings.Settings["挡脚板自重标准值"].Value;
            AQWZZ.Text = cfa.AppSettings.Settings["安全设施与安全网自重标准值"].Value;
            DJTLX.Text = cfa.AppSettings.Settings["地基土类型"].Value;
            DJCZL.Text = cfa.AppSettings.Settings["地基承载力标准值"].Value;
            JCMJ.Text = cfa.AppSettings.Settings["基础底面扩展面积"].Value;
            JCXS.Text = cfa.AppSettings.Settings["基础降低系数"].Value;
            JSCDXS.Text = cfa.AppSettings.Settings["计算长度系数"].Value;
            LGWDXS.Text = cfa.AppSettings.Settings["轴心受压立杆的稳定系数"].Value;
            _1DSGD.Text = cfa.AppSettings.Settings["该分段顶部高度"].Value;
            XGXH.Text = cfa.AppSettings.Settings["型钢型号"].Value;
            HNTBH.Text = cfa.AppSettings.Settings["楼板混凝土标号"].Value;
            JMMJ.Text = cfa.AppSettings.Settings["型钢截面面积"].Value;
            LLZL.Text = cfa.AppSettings.Settings["型钢理论质量"].Value;
            ZDBJ.Text = cfa.AppSettings.Settings["转动半径"].Value;
            JMML.Text = cfa.AppSettings.Settings["截面模量"].Value;
            ZDGL.Text = cfa.AppSettings.Settings["转动惯量"].Value;
            HNTKLQD.Text = cfa.AppSettings.Settings["混凝土抗拉强度"].Value;
            HNTKYQD.Text = cfa.AppSettings.Settings["混凝土抗压强度"].Value;
            MGXTBZ.Text = cfa.AppSettings.Settings["锚固段长度与悬挑段长度比值"].Value;
            _1LQJFY.Text = cfa.AppSettings.Settings["本段顶部风压高度变化系数"].Value;
            this.panel1.ResumeLayout();
            this.ResumeLayout();
        }

        private void Focus_Enter(object sender, EventArgs e)
        {
            this.label28.Visible = false;// 关闭保存提示
        }

        public bool IsBuildOnGround = true;
        /// <summary>
        /// 悬挑式/落地式状态按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckStateChanged(object sender, EventArgs e)
        {
            IsBuildOnGround = !IsBuildOnGround;
            this.panel1.VerticalScroll.Value = 0;
            if (IsBuildOnGround)
                SetOverHangOptionVisibility(false);
            else
                SetOverHangOptionVisibility(true);
        }

        private void SetOverHangOptionVisibility(bool visible)
        {
            label29.Visible = visible;
            label30.Visible = visible;
            label31.Visible = visible;
            label32.Visible = visible;
            label33.Visible = visible;
            label34.Visible = visible;
            label35.Visible = visible;
            label36.Visible = visible;
            label37.Visible = visible;
            label38.Visible = visible;
            label39.Visible = visible;
            label40.Visible = visible;
            _1DSGD.Visible = visible;
            XGXH.Visible = visible;
            HNTBH.Visible = visible;
            JMMJ.Visible = visible;
            LLZL.Visible = visible;
            ZDBJ.Visible = visible;
            JMML.Visible = visible;
            ZDGL.Visible = visible;
            HNTKLQD.Visible = visible;
            HNTKYQD.Visible = visible;
            MGXTBZ.Visible = visible;
            _1LQJFY.Visible = visible;
        }
    }
}