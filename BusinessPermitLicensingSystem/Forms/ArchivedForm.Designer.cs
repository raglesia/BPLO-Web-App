namespace BusinessPermitLicensingSystem.Forms
{
    partial class ArchivedForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchivedForm));
            btnRestore = new Button();
            btnClose = new Button();
            panel1 = new Panel();
            lblUsername = new Label();
            btnPaymentHistory = new Button();
            dataGridView1 = new DataGridView();
            panel2 = new Panel();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnRestore
            // 
            btnRestore.BackColor = SystemColors.GradientActiveCaption;
            btnRestore.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRestore.Image = Properties.Resources.icons8_restore_page_64;
            btnRestore.ImageAlign = ContentAlignment.MiddleLeft;
            btnRestore.Location = new Point(0, 12);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(214, 67);
            btnRestore.TabIndex = 1;
            btnRestore.Text = "          Restore Record";
            btnRestore.UseVisualStyleBackColor = false;
            btnRestore.Click += btnRestore_Click_1;
            // 
            // btnClose
            // 
            btnClose.BackColor = SystemColors.GradientActiveCaption;
            btnClose.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnClose.Image = Properties.Resources.icons8_back_64;
            btnClose.ImageAlign = ContentAlignment.MiddleLeft;
            btnClose.Location = new Point(1353, 533);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(214, 67);
            btnClose.TabIndex = 2;
            btnClose.Text = "      Back";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(lblUsername);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 606);
            panel1.Name = "panel1";
            panel1.Size = new Size(1567, 31);
            panel1.TabIndex = 4;
            // 
            // lblUsername
            // 
            lblUsername.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(3, 6);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(96, 25);
            lblUsername.TabIndex = 0;
            lblUsername.Text = "Username";
            // 
            // btnPaymentHistory
            // 
            btnPaymentHistory.BackColor = SystemColors.GradientActiveCaption;
            btnPaymentHistory.Image = Properties.Resources.icons8_payment_history_64;
            btnPaymentHistory.ImageAlign = ContentAlignment.MiddleLeft;
            btnPaymentHistory.Location = new Point(220, 12);
            btnPaymentHistory.Name = "btnPaymentHistory";
            btnPaymentHistory.Size = new Size(223, 67);
            btnPaymentHistory.TabIndex = 5;
            btnPaymentHistory.Text = "           Payment History";
            btnPaymentHistory.UseVisualStyleBackColor = false;
            btnPaymentHistory.Click += btnPaymentHistory_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(3, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(1561, 436);
            dataGridView1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(dataGridView1);
            panel2.Location = new Point(0, 85);
            panel2.Name = "panel2";
            panel2.Size = new Size(1567, 442);
            panel2.TabIndex = 6;
            // 
            // ArchivedForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            ClientSize = new Size(1567, 637);
            Controls.Add(panel2);
            Controls.Add(btnPaymentHistory);
            Controls.Add(panel1);
            Controls.Add(btnClose);
            Controls.Add(btnRestore);
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(1024, 600);
            Name = "ArchivedForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Archived ";
            Load += ArchivedForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button btnRestore;
        private Button btnClose;
        private Panel panel1;
        private Label lblUsername;
        private Button btnPaymentHistory;
        private DataGridView dataGridView1;
        private Panel panel2;
    }
}