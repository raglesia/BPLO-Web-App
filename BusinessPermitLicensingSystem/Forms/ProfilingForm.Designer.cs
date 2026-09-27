namespace BusinessPermitLicensingSystem.Forms
{
    partial class ProfilingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilingForm));
            txtFName = new TextBox();
            txtBName = new TextBox();
            txtSNumber = new TextBox();
            txtSSize = new TextBox();
            txtMRental = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            txtBIN = new TextBox();
            label7 = new Label();
            btnSave = new Button();
            button1 = new Button();
            lblUsername = new Label();
            label8 = new Label();
            cmbPaymentStatus = new ComboBox();
            dtpStartDate = new DateTimePicker();
            label9 = new Label();
            cmbBSection = new ComboBox();
            chkAdditional = new CheckBox();
            txtAdditionalCharge = new TextBox();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            groupBox3 = new GroupBox();
            txtPenalty = new TextBox();
            label11 = new Label();
            lblTotalDue = new Label();
            label10 = new Label();
            panel1 = new Panel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtFName
            // 
            txtFName.BackColor = Color.White;
            txtFName.Location = new Point(190, 60);
            txtFName.Margin = new Padding(2);
            txtFName.Name = "txtFName";
            txtFName.Size = new Size(329, 30);
            txtFName.TabIndex = 1;
            // 
            // txtBName
            // 
            txtBName.BackColor = Color.White;
            txtBName.Location = new Point(190, 90);
            txtBName.Margin = new Padding(2);
            txtBName.Name = "txtBName";
            txtBName.Size = new Size(329, 30);
            txtBName.TabIndex = 2;
            // 
            // txtSNumber
            // 
            txtSNumber.BackColor = Color.White;
            txtSNumber.Location = new Point(189, 19);
            txtSNumber.Margin = new Padding(2);
            txtSNumber.Name = "txtSNumber";
            txtSNumber.Size = new Size(329, 30);
            txtSNumber.TabIndex = 4;
            // 
            // txtSSize
            // 
            txtSSize.BackColor = Color.White;
            txtSSize.Location = new Point(189, 51);
            txtSSize.Margin = new Padding(2);
            txtSSize.Name = "txtSSize";
            txtSSize.Size = new Size(329, 30);
            txtSSize.TabIndex = 5;
            // 
            // txtMRental
            // 
            txtMRental.BackColor = Color.White;
            txtMRental.Enabled = false;
            txtMRental.Location = new Point(190, 59);
            txtMRental.Margin = new Padding(2);
            txtMRental.Name = "txtMRental";
            txtMRental.Size = new Size(329, 30);
            txtMRental.TabIndex = 8;
            txtMRental.Leave += txtMRental_Leave;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(5, 65);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(97, 23);
            label1.TabIndex = 7;
            label1.Text = "Full Name: ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(5, 94);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(134, 23);
            label2.TabIndex = 8;
            label2.Text = "Business Name: ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(5, 128);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(139, 23);
            label3.TabIndex = 9;
            label3.Text = "Business Section:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label4.Location = new Point(5, 24);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(120, 23);
            label4.TabIndex = 10;
            label4.Text = "Stall Number: ";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label5.Location = new Point(5, 56);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(86, 23);
            label5.TabIndex = 11;
            label5.Text = "Stall Size: ";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label6.Location = new Point(5, 64);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(138, 23);
            label6.TabIndex = 12;
            label6.Text = "Monthly Rental: ";
            // 
            // txtBIN
            // 
            txtBIN.BackColor = Color.White;
            txtBIN.Enabled = false;
            txtBIN.Location = new Point(190, 30);
            txtBIN.Margin = new Padding(2);
            txtBIN.Name = "txtBIN";
            txtBIN.Size = new Size(329, 30);
            txtBIN.TabIndex = 0;
            txtBIN.TextChanged += txtBIN_TextChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.Location = new Point(5, 35);
            label7.Margin = new Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new Size(46, 23);
            label7.TabIndex = 14;
            label7.Text = "SIN: ";
            // 
            // btnSave
            // 
            btnSave.BackColor = SystemColors.GradientActiveCaption;
            btnSave.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSave.Image = Properties.Resources.icons8_save_64;
            btnSave.ImageAlign = ContentAlignment.MiddleLeft;
            btnSave.Location = new Point(87, 499);
            btnSave.Margin = new Padding(2);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(175, 61);
            btnSave.TabIndex = 10;
            btnSave.Text = "          Save";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // button1
            // 
            button1.BackColor = SystemColors.GradientActiveCaption;
            button1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Image = Properties.Resources.icons8_back_64;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.Location = new Point(266, 499);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(174, 61);
            button1.TabIndex = 11;
            button1.Text = "     Back";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(2, 8);
            lblUsername.Margin = new Padding(2, 0, 2, 0);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(78, 20);
            lblUsername.TabIndex = 18;
            lblUsername.Text = "Username";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label8.Location = new Point(5, 125);
            label8.Margin = new Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new Size(134, 23);
            label8.TabIndex = 19;
            label8.Text = "Payment Status:";
            // 
            // cmbPaymentStatus
            // 
            cmbPaymentStatus.BackColor = SystemColors.GradientActiveCaption;
            cmbPaymentStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentStatus.FormattingEnabled = true;
            cmbPaymentStatus.Location = new Point(190, 118);
            cmbPaymentStatus.Margin = new Padding(2);
            cmbPaymentStatus.Name = "cmbPaymentStatus";
            cmbPaymentStatus.Size = new Size(329, 31);
            cmbPaymentStatus.TabIndex = 9;
            // 
            // dtpStartDate
            // 
            dtpStartDate.CalendarMonthBackground = Color.White;
            dtpStartDate.Location = new Point(189, 83);
            dtpStartDate.Margin = new Padding(2);
            dtpStartDate.Name = "dtpStartDate";
            dtpStartDate.Size = new Size(329, 30);
            dtpStartDate.TabIndex = 6;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.Location = new Point(5, 88);
            label9.Margin = new Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new Size(166, 23);
            label9.TabIndex = 22;
            label9.Text = "Date of Occupancy: ";
            // 
            // cmbBSection
            // 
            cmbBSection.BackColor = Color.White;
            cmbBSection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBSection.FormattingEnabled = true;
            cmbBSection.Location = new Point(190, 122);
            cmbBSection.Margin = new Padding(2);
            cmbBSection.Name = "cmbBSection";
            cmbBSection.Size = new Size(329, 31);
            cmbBSection.TabIndex = 3;
            // 
            // chkAdditional
            // 
            chkAdditional.AutoSize = true;
            chkAdditional.CheckAlign = ContentAlignment.MiddleRight;
            chkAdditional.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            chkAdditional.Location = new Point(5, 31);
            chkAdditional.Margin = new Padding(2);
            chkAdditional.Name = "chkAdditional";
            chkAdditional.Size = new Size(186, 27);
            chkAdditional.TabIndex = 24;
            chkAdditional.Text = "Additional Charges: ";
            chkAdditional.TextAlign = ContentAlignment.MiddleCenter;
            chkAdditional.UseVisualStyleBackColor = true;
            // 
            // txtAdditionalCharge
            // 
            txtAdditionalCharge.BackColor = Color.White;
            txtAdditionalCharge.Location = new Point(190, 30);
            txtAdditionalCharge.Margin = new Padding(2);
            txtAdditionalCharge.Name = "txtAdditionalCharge";
            txtAdditionalCharge.Size = new Size(329, 30);
            txtAdditionalCharge.TabIndex = 7;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cmbBSection);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(txtBIN);
            groupBox1.Controls.Add(txtFName);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(txtBName);
            groupBox1.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox1.Location = new Point(5, 10);
            groupBox1.Margin = new Padding(2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(2);
            groupBox1.Size = new Size(523, 161);
            groupBox1.TabIndex = 27;
            groupBox1.TabStop = false;
            groupBox1.Text = "STALL OWNER INFORMATION";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtSNumber);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(txtSSize);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(dtpStartDate);
            groupBox2.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox2.Location = new Point(5, 175);
            groupBox2.Margin = new Padding(2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(2);
            groupBox2.Size = new Size(523, 120);
            groupBox2.TabIndex = 28;
            groupBox2.TabStop = false;
            groupBox2.Text = "STALL INFORMATION";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(txtPenalty);
            groupBox3.Controls.Add(label11);
            groupBox3.Controls.Add(lblTotalDue);
            groupBox3.Controls.Add(label10);
            groupBox3.Controls.Add(txtAdditionalCharge);
            groupBox3.Controls.Add(chkAdditional);
            groupBox3.Controls.Add(txtMRental);
            groupBox3.Controls.Add(label8);
            groupBox3.Controls.Add(cmbPaymentStatus);
            groupBox3.Controls.Add(label6);
            groupBox3.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox3.Location = new Point(5, 300);
            groupBox3.Margin = new Padding(2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(2);
            groupBox3.Size = new Size(523, 194);
            groupBox3.TabIndex = 29;
            groupBox3.TabStop = false;
            groupBox3.Text = "RENTAL INFORMATION";
            // 
            // txtPenalty
            // 
            txtPenalty.BackColor = Color.White;
            txtPenalty.Enabled = false;
            txtPenalty.Location = new Point(190, 89);
            txtPenalty.Margin = new Padding(2);
            txtPenalty.Name = "txtPenalty";
            txtPenalty.Size = new Size(329, 30);
            txtPenalty.TabIndex = 28;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label11.Location = new Point(5, 94);
            label11.Margin = new Padding(2, 0, 2, 0);
            label11.Name = "label11";
            label11.Size = new Size(70, 23);
            label11.TabIndex = 27;
            label11.Text = "Penalty:";
            // 
            // lblTotalDue
            // 
            lblTotalDue.AutoSize = true;
            lblTotalDue.ForeColor = Color.DarkRed;
            lblTotalDue.Location = new Point(189, 158);
            lblTotalDue.Margin = new Padding(2, 0, 2, 0);
            lblTotalDue.Name = "lblTotalDue";
            lblTotalDue.Size = new Size(56, 23);
            lblTotalDue.TabIndex = 26;
            lblTotalDue.Text = "₱0.00";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label10.Location = new Point(5, 158);
            label10.Margin = new Padding(2, 0, 2, 0);
            label10.Name = "label10";
            label10.Size = new Size(153, 23);
            label10.TabIndex = 25;
            label10.Text = "Total Amount Due:";
            // 
            // panel1
            // 
            panel1.Controls.Add(lblUsername);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 564);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(536, 28);
            panel1.TabIndex = 30;
            // 
            // ProfilingForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            ClientSize = new Size(536, 592);
            Controls.Add(panel1);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(button1);
            Controls.Add(btnSave);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MaximizeBox = false;
            Name = "ProfilingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Stall Owner Profiling";
            Load += ProfilingForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TextBox txtFName;
        private TextBox txtBName;
        private TextBox txtBSection;
        private TextBox txtSNumber;
        private TextBox txtSSize;
        private TextBox txtMRental;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private TextBox txtBIN;
        private Label label7;
        private Button btnSave;
        private Button button1;
        private Label lblUsername;
        private Label label8;
        private ComboBox cmbPaymentStatus;
        private DateTimePicker dtpStartDate;
        private Label label9;
        private ComboBox cmbBSection;
        private CheckBox chkAdditional;
        private TextBox txtAdditionalCharge;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Panel panel1;
        private Label lblTotalDue;
        private Label label10;
        private Label label11;
        private TextBox txtPenalty;
    }
}