using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScaffoldTool
{
    public enum DataType : int
    {
        NONE = 1,
        INTEGER = 2,
        DOUBLE = 3
    }

    public partial class MyDataGridView : DataGridView
    {
        private System.Collections.Generic.List<string> _comboBoxText;
        private int cLeft, cTop, cWidth, cHeight;
        private int comboBoxRow, comboBoxColumn;
        private int dLeft, dTop, dWidth, dHeight;
        private int dtpRow, dtpColumn;
        private System.Collections.Generic.List<RowAttribute> _rowAttribute;

        public MyDataGridView()
        {
            InitializeComponent();
            InitializeCustomControl();
        }

        private void InitializeCustomControl()
        {
            _rowAttribute = new System.Collections.Generic.List<RowAttribute>();
            comboBox1.Parent = this;
            dateTimePicker1.Parent = this;
            comboBox1.Visible = false;
            dateTimePicker1.Visible = false;
            //this.CellBeginEdit += DataGridView1_CellBeginEdit;
            this.CellClick += MyDataGridView_CellClick;
            this.Scroll += MyDataGridView_Scroll;
            comboBox1.SelectedValueChanged += ComboBox1_SelectedValueChanged;
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
        }

        private void MyDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (comboBox1.Visible || dateTimePicker1.Visible)
            {
                comboBox1.Visible = false;
                dateTimePicker1.Visible = false;
            }
            if (_rowAttribute[e.RowIndex].ComboBoxItems != null)
            {
                _comboBoxText = _rowAttribute[e.RowIndex].ComboBoxItems;
                comboBox1.Items.Clear();
                foreach (string item in _comboBoxText)
                {
                    comboBox1.Items.Add(item);
                }

                comboBoxColumn = e.ColumnIndex;
                comboBoxRow = e.RowIndex;

                Rectangle rect = this.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                cHeight = rect.Height;
                cWidth = rect.Width;
                cLeft = rect.Left;
                cTop = rect.Top;

                comboBox1.Location = new Point(cLeft, cTop);
                comboBox1.Width = cWidth;
                comboBox1.Height = cHeight;
                comboBox1.SelectedIndex = _comboBoxText.IndexOf(this.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string);
                comboBox1.Visible = true;
                comboBox1.DroppedDown = true;
            }
            else if (_rowAttribute[e.RowIndex].IsDateTimePickerEnable == true)
            {
                dtpColumn = e.ColumnIndex;
                dtpRow = e.RowIndex;

                Rectangle rect = this.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                dHeight = rect.Height;
                dWidth = rect.Width;
                dLeft = rect.Left;
                dTop = rect.Top;

                dateTimePicker1.Location = new Point(dLeft, dTop);
                dateTimePicker1.Width = dWidth;
                dateTimePicker1.Height = dHeight;
                dateTimePicker1.Value = DateTime.Now;
                dateTimePicker1.Visible = true;
            }
        }

        private void MyDataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            comboBox1.Visible = false;
            dateTimePicker1.Visible = false;
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            this.Rows[dtpRow].Cells[dtpColumn].Value = dateTimePicker1.Value.ToLongDateString();
        }

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            this.Rows[comboBoxRow].Cells[comboBoxColumn].Value = _comboBoxText[comboBox1.SelectedIndex];
        }

        //public MyDataGridView(IContainer container)
        //{
        //    container.Add(this);
        //    InitializeComponent();
        //    InitializeCustomControl();
        //}

        public void AddNormalRow(params object[] values)
        {
            this.Rows.Add(values);
            _rowAttribute.Add(new RowAttribute { ComboBoxItems = null, IsDateTimePickerEnable = false });
        }

        public void AddComboBoxRow(System.Collections.Generic.List<string> dropItem, params object[] values)
        {
            this.Rows.Add(values);
            _rowAttribute.Add(new RowAttribute { ComboBoxItems = dropItem, IsDateTimePickerEnable = false });
        }

        public void AddDateTimePickerRow(params object[] values)
        {
            this.Rows.Add(values);
            _rowAttribute.Add(new RowAttribute { ComboBoxItems = null, IsDateTimePickerEnable = true });
        }

        //public void Add(DataType type, params object[] values)
        //{
        //    this.Rows.Add(values);
        //    switch ((int)type)
        //    {
        //        case 1:
        //            break;
        //        case 2:
        //            break;
        //        case 3:
        //            break;
        //    }
        //}

        //private bool CheckInteger(string value)
        //{
        //    return new Regex("^[0-9]+$").IsMatch(value);
        //}

        //private bool CheckDouble(string value)
        //{
        //    return new Regex("^[0-9]+(\\.\\d{1,})$").IsMatch(value);
        //}
    }

    public class RowAttribute
    {
        public List<string> ComboBoxItems { get; internal set; }
        public bool IsDateTimePickerEnable { get; internal set; }
    }
}
