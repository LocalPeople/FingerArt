using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScaffoldTool
{
    public partial class CalculationSettingForm : Form
    {
        public CalculationSettingForm()
        {
            InitializeComponent();
        }
        ProjectBasicForm projectBasicForm = new ProjectBasicForm();
        FloorScaffoldParamForm floorScaffoldParamForm = new FloorScaffoldParamForm();
        private void CalculationSettingForm_Load(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.LimeGreen;
            CalculationRules.BackColor = Color.Transparent;
            ComponentColor.BackColor = Color.Transparent;
            AttributeDisplay.BackColor = Color.Transparent;
            projectBasicForm.Show();
            Panel2.Controls.Clear();
            Panel2.Controls.Add(projectBasicForm);
        }
        private void EngineeringQuantity_Click(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.LimeGreen;
            CalculationRules.BackColor = Color.Transparent;
            ComponentColor.BackColor = Color.Transparent;
            AttributeDisplay.BackColor = Color.Transparent;
            projectBasicForm.Show();
            Panel2.Controls.Clear();
            Panel2.Controls.Add(projectBasicForm);
        }
        private void CalculationRules_Click(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.Transparent;
            CalculationRules.BackColor = Color.LimeGreen;
            ComponentColor.BackColor = Color.Transparent;
            AttributeDisplay.BackColor = Color.Transparent;
            floorScaffoldParamForm.Show();
            Panel2.Controls.Clear();
            Panel2.Controls.Add(floorScaffoldParamForm);
        }

        private void ComponentColor_Click(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.Transparent;
            CalculationRules.BackColor = Color.Transparent;
            ComponentColor.BackColor = Color.LimeGreen;
            AttributeDisplay.BackColor = Color.Transparent;
            //componentColorForm.Show();
            //Panel2.Controls.Clear();
            //Panel2.Controls.Add(componentColorForm);
        }

        private void AttributeDisplay_Click(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.Transparent;
            CalculationRules.BackColor = Color.Transparent;
            ComponentColor.BackColor = Color.Transparent;
            AttributeDisplay.BackColor = Color.LimeGreen;
            //attributeDisplayForm.Show();
            //Panel2.Controls.Clear();
            //Panel2.Controls.Add(attributeDisplayForm);
        }

        private void ComponentOptions_Click(object sender, EventArgs e)
        {
            EngineeringQuantity.BackColor = Color.Transparent;
            CalculationRules.BackColor = Color.Transparent;
            ComponentColor.BackColor = Color.Transparent;
            AttributeDisplay.BackColor = Color.Transparent;
            //componentOptionsForm.Show();
            //Panel2.Controls.Clear();
            //Panel2.Controls.Add(componentOptionsForm);
        }

        private void Determine_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
