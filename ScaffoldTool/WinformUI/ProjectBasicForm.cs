using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScaffoldTool
{
    public partial class ProjectBasicForm : UserControl
    {
        public ProjectBasicForm()
        {
            InitializeComponent();
            InitializeDataGridView();
        }

        private void InitializeDataGridView()
        {
            this.myDataGridView1.AddNormalRow("工程项目", "");
            this.myDataGridView1.AddNormalRow("工程地址", "");
            this.myDataGridView1.AddNormalRow("建设单位", "");
            this.myDataGridView1.AddNormalRow("施工单位", "");
            this.myDataGridView1.AddNormalRow("监理单位", "");
            this.myDataGridView1.AddNormalRow("编制人", "");
            this.myDataGridView1.AddDateTimePickerRow("日期", "");
            this.myDataGridView1.AddNormalRow("审核人", "");
            this.myDataGridView1.AddComboBoxRow(new System.Collections.Generic.List<string> { "", "框架结构", "钢结构", "钢筋混凝土结构" }, "结构类型", "");
            this.myDataGridView1.AddNormalRow("建筑高度(m)", "");
            this.myDataGridView1.AddNormalRow("单位工程", "");
            this.myDataGridView1.AddNormalRow("标准层高(m)", "");
            this.myDataGridView1.AddNormalRow("地上楼层数", "");
            this.myDataGridView1.AddNormalRow("项目经理", "");
            this.myDataGridView1.AddNormalRow("技术负责人", "");
            this.myDataGridView1.AddNormalRow("审核人", "");
            this.myDataGridView1.AddNormalRow("结构类型", "");
            this.myDataGridView1.AddNormalRow("建筑高度(m)", "");
            this.myDataGridView1.AddNormalRow("单位工程", "");
            this.myDataGridView1.AddNormalRow("标准层高(m)", "");
            this.myDataGridView1.AddNormalRow("地上楼层数", "");
        }
    }
}
