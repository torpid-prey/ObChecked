namespace ObChecked
{
    partial class FormMain
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
            this.BtnSelect = new System.Windows.Forms.Button();
            this.BgwFetchObjects = new System.ComponentModel.BackgroundWorker();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dgvParts = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dgvBolts = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dgvComponents = new System.Windows.Forms.DataGridView();
            this.BgwProcessParts = new System.ComponentModel.BackgroundWorker();
            this.BgwProcessBolts = new System.ComponentModel.BackgroundWorker();
            this.BgwProcessComponents = new System.ComponentModel.BackgroundWorker();
            this.progressFetch = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblParts = new System.Windows.Forms.Label();
            this.lblBolts = new System.Windows.Forms.Label();
            this.lblComponents = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.BtnClear = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParts)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBolts)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).BeginInit();
            this.SuspendLayout();
            // 
            // BtnSelect
            // 
            this.BtnSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnSelect.Location = new System.Drawing.Point(4, 12);
            this.BtnSelect.Name = "BtnSelect";
            this.BtnSelect.Size = new System.Drawing.Size(257, 46);
            this.BtnSelect.TabIndex = 0;
            this.BtnSelect.Text = "Get Selected Model Objects";
            this.BtnSelect.UseVisualStyleBackColor = true;
            this.BtnSelect.Click += new System.EventHandler(this.BtnSelect_Click);
            // 
            // BgwFetchObjects
            // 
            this.BgwFetchObjects.WorkerReportsProgress = true;
            this.BgwFetchObjects.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BgwFetchObjects_DoWork);
            this.BgwFetchObjects.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BgwFetchObjects_ProgressChanged);
            this.BgwFetchObjects.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BgwFetchObjects_RunWorkerCompleted);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(0, 64);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1660, 586);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dgvParts);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1652, 560);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Parts";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // dgvParts
            // 
            this.dgvParts.AllowUserToAddRows = false;
            this.dgvParts.AllowUserToDeleteRows = false;
            this.dgvParts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvParts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvParts.Location = new System.Drawing.Point(3, 3);
            this.dgvParts.Name = "dgvParts";
            this.dgvParts.ReadOnly = true;
            this.dgvParts.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dgvParts.RowHeadersVisible = false;
            this.dgvParts.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvParts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvParts.Size = new System.Drawing.Size(1646, 554);
            this.dgvParts.TabIndex = 2;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dgvBolts);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1652, 560);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Bolts";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvBolts
            // 
            this.dgvBolts.AllowUserToAddRows = false;
            this.dgvBolts.AllowUserToDeleteRows = false;
            this.dgvBolts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBolts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvBolts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvBolts.Location = new System.Drawing.Point(3, 3);
            this.dgvBolts.Name = "dgvBolts";
            this.dgvBolts.ReadOnly = true;
            this.dgvBolts.RowHeadersVisible = false;
            this.dgvBolts.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvBolts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBolts.Size = new System.Drawing.Size(1646, 554);
            this.dgvBolts.TabIndex = 3;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dgvComponents);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(1652, 560);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Components";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dgvComponents
            // 
            this.dgvComponents.AllowUserToAddRows = false;
            this.dgvComponents.AllowUserToDeleteRows = false;
            this.dgvComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvComponents.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvComponents.Location = new System.Drawing.Point(3, 3);
            this.dgvComponents.Name = "dgvComponents";
            this.dgvComponents.ReadOnly = true;
            this.dgvComponents.RowHeadersVisible = false;
            this.dgvComponents.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvComponents.Size = new System.Drawing.Size(1646, 554);
            this.dgvComponents.TabIndex = 4;
            // 
            // BgwProcessParts
            // 
            this.BgwProcessParts.WorkerReportsProgress = true;
            this.BgwProcessParts.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BgwProcessParts_DoWork);
            this.BgwProcessParts.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BgwProcessParts_ProgressChanged);
            this.BgwProcessParts.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BgwProcessParts_RunWorkerCompleted);
            // 
            // BgwProcessBolts
            // 
            this.BgwProcessBolts.WorkerReportsProgress = true;
            this.BgwProcessBolts.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BgwProcessBolts_DoWork);
            this.BgwProcessBolts.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BgwProcessBolts_ProgressChanged);
            this.BgwProcessBolts.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BgwProcessBolts_RunWorkerCompleted);
            // 
            // BgwProcessComponents
            // 
            this.BgwProcessComponents.WorkerReportsProgress = true;
            this.BgwProcessComponents.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BgwProcessComponents_DoWork);
            this.BgwProcessComponents.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BgwProcessComponents_ProgressChanged);
            this.BgwProcessComponents.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BgwProcessComponents_RunWorkerCompleted);
            // 
            // progressFetch
            // 
            this.progressFetch.Location = new System.Drawing.Point(346, 12);
            this.progressFetch.Name = "progressFetch";
            this.progressFetch.Size = new System.Drawing.Size(220, 31);
            this.progressFetch.TabIndex = 3;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(343, 48);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(35, 13);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "label1";
            // 
            // lblParts
            // 
            this.lblParts.AutoSize = true;
            this.lblParts.Location = new System.Drawing.Point(581, 12);
            this.lblParts.Name = "lblParts";
            this.lblParts.Size = new System.Drawing.Size(35, 13);
            this.lblParts.TabIndex = 5;
            this.lblParts.Text = "label1";
            // 
            // lblBolts
            // 
            this.lblBolts.AutoSize = true;
            this.lblBolts.Location = new System.Drawing.Point(581, 30);
            this.lblBolts.Name = "lblBolts";
            this.lblBolts.Size = new System.Drawing.Size(35, 13);
            this.lblBolts.TabIndex = 6;
            this.lblBolts.Text = "label1";
            // 
            // lblComponents
            // 
            this.lblComponents.AutoSize = true;
            this.lblComponents.Location = new System.Drawing.Point(581, 48);
            this.lblComponents.Name = "lblComponents";
            this.lblComponents.Size = new System.Drawing.Size(35, 13);
            this.lblComponents.TabIndex = 7;
            this.lblComponents.Text = "label1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1419, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(212, 35);
            this.button1.TabIndex = 8;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // BtnClear
            // 
            this.BtnClear.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.BtnClear.Location = new System.Drawing.Point(662, 20);
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.Size = new System.Drawing.Size(150, 40);
            this.BtnClear.TabIndex = 9;
            this.BtnClear.Text = "Clear";
            this.BtnClear.UseVisualStyleBackColor = true;
            this.BtnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1660, 650);
            this.Controls.Add(this.BtnClear);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lblComponents);
            this.Controls.Add(this.lblBolts);
            this.Controls.Add(this.lblParts);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressFetch);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.BtnSelect);
            this.Name = "FormMain";
            this.Text = "ObChecked";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParts)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvBolts)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnSelect;
        private System.ComponentModel.BackgroundWorker BgwFetchObjects;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.ComponentModel.BackgroundWorker BgwProcessParts;
        private System.ComponentModel.BackgroundWorker BgwProcessBolts;
        private System.ComponentModel.BackgroundWorker BgwProcessComponents;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ProgressBar progressFetch;
        private System.Windows.Forms.DataGridView dgvParts;
        private System.Windows.Forms.DataGridView dgvBolts;
        private System.Windows.Forms.DataGridView dgvComponents;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblParts;
        private System.Windows.Forms.Label lblBolts;
        private System.Windows.Forms.Label lblComponents;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnClear;
    }
}

