using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace ScaffoldTool
{
    public partial class ModelingForm : System.Windows.Forms.Form
    {
        private Level[] _levelList;
        public List<Level> ModelingLevelList { get; protected set; }
        public bool needCreateBottom = false;

        public ModelingForm()
        {
            InitializeComponent();
        }

        public ModelingForm(Level[] documentLevelList)
        {
            InitializeComponent();
            _levelList = documentLevelList;
            for (int i = 1; i < _levelList.Length; i++)
                checkedListBox1.Items.Add(_levelList[i].Name);
        }

        private int checkedItemCount = 0;

        private void checkedListBox1_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
        {
            if (e.NewValue == System.Windows.Forms.CheckState.Checked)
                checkedItemCount++;
            else if (e.NewValue == System.Windows.Forms.CheckState.Unchecked)
                checkedItemCount--;
            button1.Enabled = checkedItemCount > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ModelingLevelList = new List<Level>();
            needCreateBottom = checkedListBox1.GetItemChecked(checkedListBox1.Items.Count - 1);
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    ModelingLevelList.Add(_levelList[i + 1]);
                }
            }
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
