namespace ScaffoldTool
{
    partial class CalculationSettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CalculationSettingForm));
            this.Panel1 = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.AttributeDisplay = new System.Windows.Forms.Button();
            this.ComponentColor = new System.Windows.Forms.Button();
            this.CalculationRules = new System.Windows.Forms.Button();
            this.EngineeringQuantity = new System.Windows.Forms.Button();
            this.Panel2 = new System.Windows.Forms.Panel();
            this.Cancel = new System.Windows.Forms.Button();
            this.Application = new System.Windows.Forms.Button();
            this.Determine = new System.Windows.Forms.Button();
            this.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // Panel1
            // 
            this.Panel1.Controls.Add(this.pictureBox2);
            this.Panel1.Controls.Add(this.AttributeDisplay);
            this.Panel1.Controls.Add(this.ComponentColor);
            this.Panel1.Controls.Add(this.CalculationRules);
            this.Panel1.Controls.Add(this.EngineeringQuantity);
            this.Panel1.Location = new System.Drawing.Point(12, 12);
            this.Panel1.Name = "Panel1";
            this.Panel1.Size = new System.Drawing.Size(173, 627);
            this.Panel1.TabIndex = 0;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(16, 474);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(140, 138);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // AttributeDisplay
            // 
            this.AttributeDisplay.BackColor = System.Drawing.Color.Transparent;
            this.AttributeDisplay.FlatAppearance.BorderSize = 0;
            this.AttributeDisplay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AttributeDisplay.Font = new System.Drawing.Font("宋体", 14F);
            this.AttributeDisplay.Location = new System.Drawing.Point(6, 286);
            this.AttributeDisplay.Name = "AttributeDisplay";
            this.AttributeDisplay.Size = new System.Drawing.Size(153, 45);
            this.AttributeDisplay.TabIndex = 16;
            this.AttributeDisplay.Text = "荷载参数";
            this.AttributeDisplay.UseVisualStyleBackColor = false;
            this.AttributeDisplay.Click += new System.EventHandler(this.AttributeDisplay_Click);
            // 
            // ComponentColor
            // 
            this.ComponentColor.AutoSize = true;
            this.ComponentColor.BackColor = System.Drawing.Color.Transparent;
            this.ComponentColor.FlatAppearance.BorderSize = 0;
            this.ComponentColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ComponentColor.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ComponentColor.Location = new System.Drawing.Point(6, 206);
            this.ComponentColor.Name = "ComponentColor";
            this.ComponentColor.Size = new System.Drawing.Size(162, 45);
            this.ComponentColor.TabIndex = 15;
            this.ComponentColor.Text = "悬臂式脚手架参数";
            this.ComponentColor.UseVisualStyleBackColor = false;
            this.ComponentColor.Click += new System.EventHandler(this.ComponentColor_Click);
            // 
            // CalculationRules
            // 
            this.CalculationRules.AutoSize = true;
            this.CalculationRules.BackColor = System.Drawing.Color.Transparent;
            this.CalculationRules.FlatAppearance.BorderSize = 0;
            this.CalculationRules.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CalculationRules.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.CalculationRules.Location = new System.Drawing.Point(6, 126);
            this.CalculationRules.Name = "CalculationRules";
            this.CalculationRules.Size = new System.Drawing.Size(157, 45);
            this.CalculationRules.TabIndex = 14;
            this.CalculationRules.Text = "落地式脚手架参数";
            this.CalculationRules.UseVisualStyleBackColor = false;
            this.CalculationRules.Click += new System.EventHandler(this.CalculationRules_Click);
            // 
            // EngineeringQuantity
            // 
            this.EngineeringQuantity.AutoSize = true;
            this.EngineeringQuantity.BackColor = System.Drawing.Color.Transparent;
            this.EngineeringQuantity.FlatAppearance.BorderSize = 0;
            this.EngineeringQuantity.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.EngineeringQuantity.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.EngineeringQuantity.Location = new System.Drawing.Point(6, 46);
            this.EngineeringQuantity.Name = "EngineeringQuantity";
            this.EngineeringQuantity.Size = new System.Drawing.Size(153, 45);
            this.EngineeringQuantity.TabIndex = 13;
            this.EngineeringQuantity.Text = "工程信息";
            this.EngineeringQuantity.UseVisualStyleBackColor = false;
            this.EngineeringQuantity.Click += new System.EventHandler(this.EngineeringQuantity_Click);
            // 
            // Panel2
            // 
            this.Panel2.Location = new System.Drawing.Point(201, 12);
            this.Panel2.Name = "Panel2";
            this.Panel2.Size = new System.Drawing.Size(798, 612);
            this.Panel2.TabIndex = 1;
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Cancel.Location = new System.Drawing.Point(741, 630);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 13;
            this.Cancel.Text = "取消";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // Application
            // 
            this.Application.Location = new System.Drawing.Point(894, 630);
            this.Application.Name = "Application";
            this.Application.Size = new System.Drawing.Size(75, 23);
            this.Application.TabIndex = 12;
            this.Application.Text = "应用";
            this.Application.UseVisualStyleBackColor = true;
            // 
            // Determine
            // 
            this.Determine.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Determine.Location = new System.Drawing.Point(643, 630);
            this.Determine.Name = "Determine";
            this.Determine.Size = new System.Drawing.Size(75, 23);
            this.Determine.TabIndex = 11;
            this.Determine.Text = "确定";
            this.Determine.UseVisualStyleBackColor = true;
            this.Determine.Click += new System.EventHandler(this.Determine_Click);
            // 
            // CalculationSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1011, 665);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Application);
            this.Controls.Add(this.Panel2);
            this.Controls.Add(this.Determine);
            this.Controls.Add(this.Panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "CalculationSettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "算量设置";
            this.Load += new System.EventHandler(this.CalculationSettingForm_Load);
            this.Panel1.ResumeLayout(false);
            this.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel Panel1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button AttributeDisplay;
        private System.Windows.Forms.Button ComponentColor;
        private System.Windows.Forms.Button CalculationRules;
        private System.Windows.Forms.Button EngineeringQuantity;
        private System.Windows.Forms.Panel Panel2;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button Application;
        private System.Windows.Forms.Button Determine;
    }
}