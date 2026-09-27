namespace BusinessPermitLicensingSystem.Forms
{
    partial class VehiclePermitLists
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
            dataGridView1 = new DataGridView();
            txtSearch = new TextBox();
            lblTotalRecords = new Label();
            lblUsername = new Label();
            lblTotalVehicles = new Label();
            lblTotalPaid = new Label();
            lblTotalUnpaid = new Label();
            btnPaymentHistory = new Button();
            btnGenerateReceipt = new Button();
            btnArchive = new Button();
            btnImport = new Button();
            btnReport = new Button();
            btnExport = new Button();
            btnMenu = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            panel2 = new Panel();
            label6 = new Label();
            label5 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(2, 112);
            dataGridView1.Margin = new Padding(2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(1896, 810);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            dataGridView1.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = SystemColors.GradientActiveCaption;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtSearch.Location = new Point(2, 77);
            txtSearch.Margin = new Padding(2);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search Records:";
            txtSearch.Size = new Size(1896, 27);
            txtSearch.TabIndex = 1;
            // 
            // lblTotalRecords
            // 
            lblTotalRecords.AutoSize = true;
            lblTotalRecords.Dock = DockStyle.Right;
            lblTotalRecords.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalRecords.Location = new Point(1748, 0);
            lblTotalRecords.Margin = new Padding(2, 0, 2, 0);
            lblTotalRecords.Name = "lblTotalRecords";
            lblTotalRecords.Size = new Size(148, 20);
            lblTotalRecords.TabIndex = 2;
            lblTotalRecords.Text = "Total Records: 00000";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Dock = DockStyle.Left;
            lblUsername.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(0, 0);
            lblUsername.Margin = new Padding(2, 0, 2, 0);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(78, 20);
            lblUsername.TabIndex = 3;
            lblUsername.Text = "Username";
            // 
            // lblTotalVehicles
            // 
            lblTotalVehicles.AutoSize = true;
            lblTotalVehicles.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalVehicles.ForeColor = Color.DarkGreen;
            lblTotalVehicles.Location = new Point(555, 4);
            lblTotalVehicles.Margin = new Padding(2, 0, 2, 0);
            lblTotalVehicles.Name = "lblTotalVehicles";
            lblTotalVehicles.Size = new Size(41, 20);
            lblTotalVehicles.TabIndex = 15;
            lblTotalVehicles.Text = "0000";
            // 
            // lblTotalPaid
            // 
            lblTotalPaid.AutoSize = true;
            lblTotalPaid.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalPaid.ForeColor = Color.DarkBlue;
            lblTotalPaid.Location = new Point(738, 4);
            lblTotalPaid.Margin = new Padding(2, 0, 2, 0);
            lblTotalPaid.Name = "lblTotalPaid";
            lblTotalPaid.Size = new Size(41, 20);
            lblTotalPaid.TabIndex = 16;
            lblTotalPaid.Text = "0000";
            lblTotalPaid.Click += lblTotalPaid_Click;
            // 
            // lblTotalUnpaid
            // 
            lblTotalUnpaid.AutoSize = true;
            lblTotalUnpaid.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTotalUnpaid.ForeColor = Color.Red;
            lblTotalUnpaid.Location = new Point(938, 4);
            lblTotalUnpaid.Margin = new Padding(2, 0, 2, 0);
            lblTotalUnpaid.Name = "lblTotalUnpaid";
            lblTotalUnpaid.Size = new Size(41, 20);
            lblTotalUnpaid.TabIndex = 17;
            lblTotalUnpaid.Text = "0000";
            // 
            // btnPaymentHistory
            // 
            btnPaymentHistory.BackColor = SystemColors.GradientActiveCaption;
            btnPaymentHistory.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnPaymentHistory.Image = Properties.Resources.icons8_payment_history_64;
            btnPaymentHistory.ImageAlign = ContentAlignment.MiddleLeft;
            btnPaymentHistory.Location = new Point(225, 0);
            btnPaymentHistory.Margin = new Padding(2);
            btnPaymentHistory.Name = "btnPaymentHistory";
            btnPaymentHistory.Size = new Size(219, 68);
            btnPaymentHistory.TabIndex = 2;
            btnPaymentHistory.Text = "             Payment History";
            btnPaymentHistory.UseVisualStyleBackColor = false;
            // 
            // btnGenerateReceipt
            // 
            btnGenerateReceipt.BackColor = SystemColors.GradientActiveCaption;
            btnGenerateReceipt.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnGenerateReceipt.Image = Properties.Resources.icons8_receipt_64;
            btnGenerateReceipt.ImageAlign = ContentAlignment.MiddleLeft;
            btnGenerateReceipt.Location = new Point(2, 0);
            btnGenerateReceipt.Margin = new Padding(2);
            btnGenerateReceipt.Name = "btnGenerateReceipt";
            btnGenerateReceipt.Size = new Size(219, 68);
            btnGenerateReceipt.TabIndex = 1;
            btnGenerateReceipt.Text = "           Generate Receipt";
            btnGenerateReceipt.UseVisualStyleBackColor = false;
            // 
            // btnArchive
            // 
            btnArchive.BackColor = SystemColors.GradientActiveCaption;
            btnArchive.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnArchive.Image = Properties.Resources.icons8_archive_64;
            btnArchive.ImageAlign = ContentAlignment.MiddleLeft;
            btnArchive.Location = new Point(448, 0);
            btnArchive.Margin = new Padding(2);
            btnArchive.Name = "btnArchive";
            btnArchive.Size = new Size(219, 68);
            btnArchive.TabIndex = 3;
            btnArchive.Text = "            Archive Record";
            btnArchive.UseVisualStyleBackColor = false;
            btnArchive.Click += btnArchive_Click;
            // 
            // btnImport
            // 
            btnImport.BackColor = SystemColors.GradientActiveCaption;
            btnImport.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnImport.Image = Properties.Resources.icons8_import_csv_64;
            btnImport.ImageAlign = ContentAlignment.MiddleLeft;
            btnImport.Location = new Point(1229, 0);
            btnImport.Margin = new Padding(2);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(219, 68);
            btnImport.TabIndex = 6;
            btnImport.Text = "              Import Records";
            btnImport.UseVisualStyleBackColor = false;
            btnImport.Click += btnImport_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = SystemColors.GradientActiveCaption;
            btnReport.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnReport.Image = Properties.Resources.icons8_ledger_64;
            btnReport.ImageAlign = ContentAlignment.MiddleLeft;
            btnReport.Location = new Point(1006, 0);
            btnReport.Margin = new Padding(2);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(219, 68);
            btnReport.TabIndex = 5;
            btnReport.Text = "              Collection Report";
            btnReport.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            btnExport.BackColor = SystemColors.GradientActiveCaption;
            btnExport.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExport.Image = Properties.Resources.icons8_xls_64;
            btnExport.ImageAlign = ContentAlignment.MiddleLeft;
            btnExport.Location = new Point(1452, 0);
            btnExport.Margin = new Padding(2);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(219, 68);
            btnExport.TabIndex = 7;
            btnExport.Text = "            Export Records";
            btnExport.UseVisualStyleBackColor = false;
            btnExport.Click += btnExport_Click;
            // 
            // btnMenu
            // 
            btnMenu.BackColor = SystemColors.GradientActiveCaption;
            btnMenu.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMenu.Image = Properties.Resources.icons8_menu_64;
            btnMenu.ImageAlign = ContentAlignment.MiddleLeft;
            btnMenu.Location = new Point(1675, 0);
            btnMenu.Margin = new Padding(2);
            btnMenu.Name = "btnMenu";
            btnMenu.Size = new Size(219, 68);
            btnMenu.TabIndex = 8;
            btnMenu.Text = "        Main Menu";
            btnMenu.UseVisualStyleBackColor = false;
            btnMenu.Click += btnMenu_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Controls.Add(dataGridView1, 0, 2);
            tableLayoutPanel1.Controls.Add(txtSearch, 0, 1);
            tableLayoutPanel1.Controls.Add(panel2, 0, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.Size = new Size(1900, 959);
            tableLayoutPanel1.TabIndex = 26;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnPaymentHistory);
            panel1.Controls.Add(btnMenu);
            panel1.Controls.Add(btnGenerateReceipt);
            panel1.Controls.Add(btnExport);
            panel1.Controls.Add(btnArchive);
            panel1.Controls.Add(btnReport);
            panel1.Controls.Add(btnImport);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(2, 2);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(1896, 71);
            panel1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(label6);
            panel2.Controls.Add(label5);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(lblUsername);
            panel2.Controls.Add(lblTotalUnpaid);
            panel2.Controls.Add(lblTotalRecords);
            panel2.Controls.Add(lblTotalVehicles);
            panel2.Controls.Add(lblTotalPaid);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(2, 926);
            panel2.Margin = new Padding(2);
            panel2.Name = "panel2";
            panel2.Size = new Size(1896, 31);
            panel2.TabIndex = 2;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(816, 4);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(14, 20);
            label6.TabIndex = 23;
            label6.Text = "|";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.Location = new Point(627, 4);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(14, 20);
            label5.TabIndex = 22;
            label5.Text = "|";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(834, 4);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(100, 20);
            label3.TabIndex = 20;
            label3.Text = "Total Unpaid:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(446, 4);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(105, 20);
            label2.TabIndex = 19;
            label2.Text = "Total Vehicles:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(654, 4);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(80, 20);
            label1.TabIndex = 18;
            label1.Text = "Total Paid:";
            // 
            // VehiclePermitLists
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            ClientSize = new Size(1900, 959);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimumSize = new Size(1918, 1006);
            Name = "VehiclePermitLists";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Special Vehicle Permit List";
            WindowState = FormWindowState.Maximized;
            Load += VehiclePermitLists_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private TextBox txtSearch;
        private Label lblTotalRecords;
        private Label lblUsername;
        private Label lblTotalVehicles;
        private Label lblTotalPaid;
        private Label lblTotalUnpaid;
        private Button btnPaymentHistory;
        private Button btnGenerateReceipt;
        private Button btnArchive;
        private Button btnImport;
        private Button btnReport;
        private Button btnExport;
        private Button btnMenu;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private Panel panel2;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label label6;
        private Label label5;
    }
}