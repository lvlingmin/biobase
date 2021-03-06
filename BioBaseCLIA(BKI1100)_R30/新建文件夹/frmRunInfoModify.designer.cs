namespace BioBaseCLIA.Run
{
    partial class frmRunInfoModify
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmRunInfoModify));
            this.dgvSpRunInfoList = new System.Windows.Forms.DataGridView();
            this.Position = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SampleNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SampleType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DilutionTimes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiluteName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.fbtnModify = new BioBaseCLIA.CustomControl.FunctionButton(this.components);
            this.fbtnOK = new BioBaseCLIA.CustomControl.FunctionButton(this.components);
            this.chkEmergency = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtDilutionTimes = new BioBaseCLIA.CustomControl.userNumTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSpRunInfoList)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSpRunInfoList
            // 
            this.dgvSpRunInfoList.AllowUserToAddRows = false;
            this.dgvSpRunInfoList.AllowUserToDeleteRows = false;
            this.dgvSpRunInfoList.AllowUserToResizeRows = false;
            this.dgvSpRunInfoList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSpRunInfoList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSpRunInfoList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSpRunInfoList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Position,
            this.SampleNo,
            this.SampleType,
            this.dataGridViewTextBoxColumn1,
            this.DilutionTimes,
            this.DiluteName,
            this.dataGridViewTextBoxColumn2});
            resources.ApplyResources(this.dgvSpRunInfoList, "dgvSpRunInfoList");
            this.dgvSpRunInfoList.Name = "dgvSpRunInfoList";
            this.dgvSpRunInfoList.ReadOnly = true;
            this.dgvSpRunInfoList.RowHeadersVisible = false;
            this.dgvSpRunInfoList.RowTemplate.Height = 23;
            this.dgvSpRunInfoList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSpRunInfoList.SelectionChanged += new System.EventHandler(this.dgvSpRunInfoList_SelectionChanged);
            // 
            // Position
            // 
            this.Position.DataPropertyName = "Position";
            resources.ApplyResources(this.Position, "Position");
            this.Position.Name = "Position";
            this.Position.ReadOnly = true;
            // 
            // SampleNo
            // 
            this.SampleNo.DataPropertyName = "SampleNo";
            resources.ApplyResources(this.SampleNo, "SampleNo");
            this.SampleNo.Name = "SampleNo";
            this.SampleNo.ReadOnly = true;
            // 
            // SampleType
            // 
            this.SampleType.DataPropertyName = "SampleType";
            resources.ApplyResources(this.SampleType, "SampleType");
            this.SampleType.Name = "SampleType";
            this.SampleType.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "ItemName";
            resources.ApplyResources(this.dataGridViewTextBoxColumn1, "dataGridViewTextBoxColumn1");
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // DilutionTimes
            // 
            this.DilutionTimes.DataPropertyName = "DilutionTimes";
            resources.ApplyResources(this.DilutionTimes, "DilutionTimes");
            this.DilutionTimes.Name = "DilutionTimes";
            this.DilutionTimes.ReadOnly = true;
            // 
            // DiluteName
            // 
            this.DiluteName.DataPropertyName = "DiluteName";
            resources.ApplyResources(this.DiluteName, "DiluteName");
            this.DiluteName.Name = "DiluteName";
            this.DiluteName.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Emergency";
            resources.ApplyResources(this.dataGridViewTextBoxColumn2, "dataGridViewTextBoxColumn2");
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.fbtnModify);
            this.groupBox1.Controls.Add(this.fbtnOK);
            this.groupBox1.Controls.Add(this.chkEmergency);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtDilutionTimes);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // fbtnModify
            // 
            this.fbtnModify.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.fbtnModify, "fbtnModify");
            this.fbtnModify.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.fbtnModify.FlatAppearance.BorderSize = 0;
            this.fbtnModify.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.fbtnModify.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.fbtnModify.Name = "fbtnModify";
            this.fbtnModify.UseVisualStyleBackColor = false;
            this.fbtnModify.Click += new System.EventHandler(this.fbtnModify_Click);
            // 
            // fbtnOK
            // 
            this.fbtnOK.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.fbtnOK, "fbtnOK");
            this.fbtnOK.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.fbtnOK.FlatAppearance.BorderSize = 0;
            this.fbtnOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.fbtnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.fbtnOK.Name = "fbtnOK";
            this.fbtnOK.UseVisualStyleBackColor = false;
            this.fbtnOK.Click += new System.EventHandler(this.fbtnOK_Click);
            // 
            // chkEmergency
            // 
            resources.ApplyResources(this.chkEmergency, "chkEmergency");
            this.chkEmergency.Name = "chkEmergency";
            this.chkEmergency.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // txtDilutionTimes
            // 
            resources.ApplyResources(this.txtDilutionTimes, "txtDilutionTimes");
            this.txtDilutionTimes.IsNull = false;
            this.txtDilutionTimes.MaxValue = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtDilutionTimes.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtDilutionTimes.Name = "txtDilutionTimes";
            // 
            // frmRunInfoModify
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dgvSpRunInfoList);
            this.Name = "frmRunInfoModify";
            this.Load += new System.EventHandler(this.frmRunInfoModify_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSpRunInfoList)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSpRunInfoList;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private CustomControl.userNumTextBox txtDilutionTimes;
        private System.Windows.Forms.CheckBox chkEmergency;
        private CustomControl.FunctionButton fbtnOK;
        private CustomControl.FunctionButton fbtnModify;
        private System.Windows.Forms.DataGridViewTextBoxColumn Position;
        private System.Windows.Forms.DataGridViewTextBoxColumn SampleNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn SampleType;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn DilutionTimes;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiluteName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
    }
}