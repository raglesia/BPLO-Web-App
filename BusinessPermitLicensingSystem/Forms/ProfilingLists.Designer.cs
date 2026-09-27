namespace BusinessPermitLicensingSystem
{
    partial class ProfilingLists
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilingLists));
            panelFilters = new Panel();
            txtSearch = new TextBox();
            dataGridView1 = new DataGridView();
            panelButtons = new Panel();
            btnImport = new Button();
            btnMonthlyReport = new Button();
            btnArchive = new Button();
            button2 = new Button();
            btnPaymentHistory = new Button();
            button1 = new Button();
            btnExport = new Button();
            lblTotalRecords = new Label();
            lblUsername = new Label();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            panel2 = new Panel();
            label6 = new Label();
            lblTotalPenalty = new Label();
            label5 = new Label();
            lblTotalUncollected = new Label();
            label4 = new Label();
            label8 = new Label();
            lblTotalCollected = new Label();
            label3 = new Label();
            label9 = new Label();
            lblTotalUnpaid = new Label();
            label2 = new Label();
            label10 = new Label();
            lblTotalPaid = new Label();
            label11 = new Label();
            panelFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panelButtons.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panelFilters
            // 
            panelFilters.Controls.Add(txtSearch);
            panelFilters.Dock = DockStyle.Fill;
            panelFilters.Location = new Point(3, 78);
            panelFilters.Name = "panelFilters";
            panelFilters.Size = new Size(1894, 39);
            panelFilters.TabIndex = 39;
            // 
            // txtSearch
            // 
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.BackColor = SystemColors.GradientActiveCaption;
            txtSearch.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtSearch.Location = new Point(3, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search Record: ";
            txtSearch.Size = new Size(1888, 30);
            txtSearch.TabIndex = 0;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI Semibold", 10.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 123);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(1894, 798);
            dataGridView1.TabIndex = 23;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            // 
            // panelButtons
            // 
            panelButtons.Controls.Add(btnImport);
            panelButtons.Controls.Add(btnMonthlyReport);
            panelButtons.Controls.Add(btnArchive);
            panelButtons.Controls.Add(button2);
            panelButtons.Controls.Add(btnPaymentHistory);
            panelButtons.Controls.Add(button1);
            panelButtons.Controls.Add(btnExport);
            panelButtons.Dock = DockStyle.Fill;
            panelButtons.Location = new Point(3, 3);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(1894, 69);
            panelButtons.TabIndex = 38;
            // 
            // btnImport
            // 
            btnImport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnImport.BackColor = SystemColors.GradientActiveCaption;
            btnImport.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnImport.Image = Properties.Resources.icons8_import_csv_64;
            btnImport.ImageAlign = ContentAlignment.MiddleLeft;
            btnImport.Location = new Point(1222, 0);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(219, 68);
            btnImport.TabIndex = 5;
            btnImport.Text = "            Import Records\r\n";
            btnImport.UseVisualStyleBackColor = false;
            btnImport.Click += btnImport_Click;
            // 
            // btnMonthlyReport
            // 
            btnMonthlyReport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMonthlyReport.BackColor = SystemColors.GradientActiveCaption;
            btnMonthlyReport.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnMonthlyReport.Image = Properties.Resources.icons8_ledger_64;
            btnMonthlyReport.ImageAlign = ContentAlignment.MiddleLeft;
            btnMonthlyReport.Location = new Point(997, 0);
            btnMonthlyReport.Name = "btnMonthlyReport";
            btnMonthlyReport.Size = new Size(219, 68);
            btnMonthlyReport.TabIndex = 6;
            btnMonthlyReport.Text = "             Collection Report\r\n";
            btnMonthlyReport.UseVisualStyleBackColor = false;
            btnMonthlyReport.Click += btnMonthlyReport_Click_1;
            // 
            // btnArchive
            // 
            btnArchive.BackColor = SystemColors.GradientActiveCaption;
            btnArchive.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnArchive.Image = Properties.Resources.icons8_archive_64;
            btnArchive.ImageAlign = ContentAlignment.MiddleLeft;
            btnArchive.Location = new Point(453, 0);
            btnArchive.Name = "btnArchive";
            btnArchive.Size = new Size(219, 68);
            btnArchive.TabIndex = 3;
            btnArchive.Text = "            Archive Record";
            btnArchive.UseVisualStyleBackColor = false;
            btnArchive.Click += btnArchive_Click;
            // 
            // button2
            // 
            button2.BackColor = SystemColors.GradientActiveCaption;
            button2.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.Image = Properties.Resources.icons8_receipt_64;
            button2.ImageAlign = ContentAlignment.MiddleLeft;
            button2.Location = new Point(3, 0);
            button2.Name = "button2";
            button2.Size = new Size(219, 68);
            button2.TabIndex = 1;
            button2.Text = "            Generate Receipt";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // btnPaymentHistory
            // 
            btnPaymentHistory.BackColor = SystemColors.GradientActiveCaption;
            btnPaymentHistory.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnPaymentHistory.Image = Properties.Resources.icons8_payment_history_64;
            btnPaymentHistory.ImageAlign = ContentAlignment.MiddleLeft;
            btnPaymentHistory.Location = new Point(228, 0);
            btnPaymentHistory.Name = "btnPaymentHistory";
            btnPaymentHistory.Size = new Size(219, 68);
            btnPaymentHistory.TabIndex = 2;
            btnPaymentHistory.Text = "              Payment History";
            btnPaymentHistory.UseVisualStyleBackColor = false;
            btnPaymentHistory.Click += btnPaymentHistory_Click;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.BackColor = SystemColors.GradientActiveCaption;
            button1.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            button1.Image = Properties.Resources.icons8_menu_64;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.Location = new Point(1672, 0);
            button1.Name = "button1";
            button1.Size = new Size(219, 68);
            button1.TabIndex = 8;
            button1.Text = "            Main Menu";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // btnExport
            // 
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.BackColor = SystemColors.GradientActiveCaption;
            btnExport.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnExport.Image = Properties.Resources.icons8_xls_64;
            btnExport.ImageAlign = ContentAlignment.MiddleLeft;
            btnExport.Location = new Point(1447, 0);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(219, 68);
            btnExport.TabIndex = 7;
            btnExport.Text = "            Export Report";
            btnExport.UseVisualStyleBackColor = false;
            btnExport.Click += btnExport_Click;
            // 
            // lblTotalRecords
            // 
            lblTotalRecords.AutoSize = true;
            lblTotalRecords.Dock = DockStyle.Right;
            lblTotalRecords.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalRecords.Location = new Point(1728, 0);
            lblTotalRecords.Name = "lblTotalRecords";
            lblTotalRecords.Size = new Size(166, 23);
            lblTotalRecords.TabIndex = 41;
            lblTotalRecords.Text = "Total Records: 00000";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Dock = DockStyle.Left;
            lblUsername.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(0, 0);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(92, 23);
            lblUsername.TabIndex = 34;
            lblUsername.Text = "Username ";
            lblUsername.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(panelButtons, 0, 0);
            tableLayoutPanel1.Controls.Add(dataGridView1, 0, 2);
            tableLayoutPanel1.Controls.Add(panelFilters, 0, 1);
            tableLayoutPanel1.Controls.Add(panel1, 0, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(1900, 959);
            tableLayoutPanel1.TabIndex = 40;
            // 
            // panel1
            // 
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(lblTotalRecords);
            panel1.Controls.Add(lblUsername);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 927);
            panel1.Name = "panel1";
            panel1.Size = new Size(1894, 29);
            panel1.TabIndex = 40;
            // 
            // panel2
            // 
            panel2.AutoSize = true;
            panel2.Controls.Add(label6);
            panel2.Controls.Add(lblTotalPenalty);
            panel2.Controls.Add(label5);
            panel2.Controls.Add(lblTotalUncollected);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(label8);
            panel2.Controls.Add(lblTotalCollected);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(label9);
            panel2.Controls.Add(lblTotalUnpaid);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(label10);
            panel2.Controls.Add(lblTotalPaid);
            panel2.Controls.Add(label11);
            panel2.Location = new Point(283, -3);
            panel2.Name = "panel2";
            panel2.Size = new Size(1343, 37);
            panel2.TabIndex = 41;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(1065, 3);
            label6.Name = "label6";
            label6.Size = new Size(111, 23);
            label6.TabIndex = 47;
            label6.Text = "Total Penalty:";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalPenalty
            // 
            lblTotalPenalty.AutoSize = true;
            lblTotalPenalty.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPenalty.ForeColor = Color.DarkBlue;
            lblTotalPenalty.Location = new Point(1182, 3);
            lblTotalPenalty.Name = "lblTotalPenalty";
            lblTotalPenalty.Size = new Size(113, 23);
            lblTotalPenalty.TabIndex = 53;
            lblTotalPenalty.Text = "P0,000,000.00";
            lblTotalPenalty.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            label5.Location = new Point(745, 4);
            label5.Name = "label5";
            label5.Size = new Size(145, 23);
            label5.TabIndex = 46;
            label5.Text = "Total Uncollected:";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalUncollected
            // 
            lblTotalUncollected.AutoSize = true;
            lblTotalUncollected.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalUncollected.ForeColor = Color.Red;
            lblTotalUncollected.Location = new Point(900, 3);
            lblTotalUncollected.Name = "lblTotalUncollected";
            lblTotalUncollected.Size = new Size(113, 23);
            lblTotalUncollected.TabIndex = 52;
            lblTotalUncollected.Text = "P0,000,000.00";
            lblTotalUncollected.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            label4.Location = new Point(446, 4);
            label4.Name = "label4";
            label4.Size = new Size(126, 23);
            label4.TabIndex = 45;
            label4.Text = "Total Collected:";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label8.Location = new Point(1042, 4);
            label8.Name = "label8";
            label8.Size = new Size(13, 20);
            label8.TabIndex = 44;
            label8.Text = "|";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalCollected
            // 
            lblTotalCollected.AutoSize = true;
            lblTotalCollected.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            lblTotalCollected.ForeColor = Color.DarkGreen;
            lblTotalCollected.Location = new Point(582, 4);
            lblTotalCollected.Name = "lblTotalCollected";
            lblTotalCollected.Size = new Size(113, 23);
            lblTotalCollected.TabIndex = 51;
            lblTotalCollected.Text = "P0,000,000.00";
            lblTotalCollected.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            label3.Location = new Point(238, 4);
            label3.Name = "label3";
            label3.Size = new Size(110, 23);
            label3.TabIndex = 44;
            label3.Text = "Total Unpaid:";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label9.Location = new Point(722, 4);
            label9.Name = "label9";
            label9.Size = new Size(13, 20);
            label9.TabIndex = 54;
            label9.Text = "|";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalUnpaid
            // 
            lblTotalUnpaid.AutoSize = true;
            lblTotalUnpaid.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            lblTotalUnpaid.ForeColor = Color.Red;
            lblTotalUnpaid.Location = new Point(358, 4);
            lblTotalUnpaid.Name = "lblTotalUnpaid";
            lblTotalUnpaid.Size = new Size(46, 23);
            lblTotalUnpaid.TabIndex = 50;
            lblTotalUnpaid.Text = "0000";
            lblTotalUnpaid.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            label2.Location = new Point(55, 4);
            label2.Name = "label2";
            label2.Size = new Size(88, 23);
            label2.TabIndex = 43;
            label2.Text = "Total Paid:";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label10.Location = new Point(423, 3);
            label10.Name = "label10";
            label10.Size = new Size(13, 20);
            label10.TabIndex = 55;
            label10.Text = "|";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalPaid
            // 
            lblTotalPaid.AutoSize = true;
            lblTotalPaid.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            lblTotalPaid.ForeColor = Color.DarkGreen;
            lblTotalPaid.Location = new Point(154, 4);
            lblTotalPaid.Name = "lblTotalPaid";
            lblTotalPaid.Size = new Size(46, 23);
            lblTotalPaid.TabIndex = 49;
            lblTotalPaid.Text = "0000";
            lblTotalPaid.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label11.Location = new Point(215, 4);
            label11.Name = "label11";
            label11.Size = new Size(13, 20);
            label11.TabIndex = 56;
            label11.Text = "|";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ProfilingLists
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            ClientSize = new Size(1900, 959);
            Controls.Add(tableLayoutPanel1);
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(1918, 1006);
            Name = "ProfilingLists";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Stall Owners List";
            WindowState = FormWindowState.Maximized;
            Load += ProfilingLists_Load;
            panelFilters.ResumeLayout(false);
            panelFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panelButtons.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelFilters;
        private TextBox txtSearch;
        private DataGridView dataGridView1;
        private Panel panelButtons;
        private Button btnArchive;
        private Label lblUsername;
        private Button button2;
        private Button btnPaymentHistory;
        private Button button1;
        private Button btnExport;
        private TableLayoutPanel tableLayoutPanel1;
        private Label lblTotalRecords;
        private Button btnMonthlyReport;
        private Button btnImport;
        private Panel panel1;
        private Label lblTotalPenalty;
        private Label lblTotalUncollected;
        private Label lblTotalCollected;
        private Label lblTotalUnpaid;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label2;
        private Label lblTotalPaid;
        private Label label3;
        private Label label8;
        private Label label9;
        private Label label11;
        private Label label10;
        private Panel panel2;
    }
}