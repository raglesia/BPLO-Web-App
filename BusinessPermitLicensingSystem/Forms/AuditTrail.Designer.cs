namespace BusinessPermitLicensingSystem.Forms
{
    partial class AuditTrail
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuditTrail));
            dtAudit = new DataGridView();
            button1 = new Button();
            radioUsers = new RadioButton();
            radioProfiling = new RadioButton();
            lblUsername = new Label();
            panel1 = new Panel();
            ((System.ComponentModel.ISupportInitialize)dtAudit).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // dtAudit
            // 
            dtAudit.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dtAudit.Location = new Point(12, 50);
            dtAudit.Name = "dtAudit";
            dtAudit.RowHeadersWidth = 62;
            dtAudit.Size = new Size(1573, 418);
            dtAudit.TabIndex = 0;
            // 
            // button1
            // 
            button1.BackColor = SystemColors.GradientActiveCaption;
            button1.Image = Properties.Resources.icons8_menu_64;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.Location = new Point(12, 471);
            button1.Name = "button1";
            button1.Size = new Size(168, 65);
            button1.TabIndex = 3;
            button1.Text = "Main Menu";
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // radioUsers
            // 
            radioUsers.AutoSize = true;
            radioUsers.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            radioUsers.Location = new Point(12, 12);
            radioUsers.Name = "radioUsers";
            radioUsers.Size = new Size(134, 29);
            radioUsers.TabIndex = 1;
            radioUsers.TabStop = true;
            radioUsers.Text = "Users Audit";
            radioUsers.UseVisualStyleBackColor = true;
            radioUsers.CheckedChanged += radioUsers_CheckedChanged;
            // 
            // radioProfiling
            // 
            radioProfiling.AutoSize = true;
            radioProfiling.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            radioProfiling.Location = new Point(152, 12);
            radioProfiling.Name = "radioProfiling";
            radioProfiling.Size = new Size(160, 29);
            radioProfiling.TabIndex = 2;
            radioProfiling.TabStop = true;
            radioProfiling.Text = "Profiling Audit";
            radioProfiling.UseVisualStyleBackColor = true;
            radioProfiling.CheckedChanged += radioProfiling_CheckedChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Dock = DockStyle.Bottom;
            lblUsername.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(0, 9);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(69, 25);
            lblUsername.TabIndex = 4;
            lblUsername.Text = "lbluser";
            // 
            // panel1
            // 
            panel1.Controls.Add(lblUsername);
            panel1.Location = new Point(12, 542);
            panel1.Name = "panel1";
            panel1.Size = new Size(1573, 34);
            panel1.TabIndex = 5;
            // 
            // AuditTrail
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            ClientSize = new Size(1594, 578);
            Controls.Add(panel1);
            Controls.Add(radioProfiling);
            Controls.Add(radioUsers);
            Controls.Add(button1);
            Controls.Add(dtAudit);
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "AuditTrail";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Audit Logs";
            Load += AuditTrail_Load;
            ((System.ComponentModel.ISupportInitialize)dtAudit).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dtAudit;
        private Button button1;
        private RadioButton radioUsers;
        private RadioButton radioProfiling;
        private Label lblUsername;
        private Panel panel1;
    }
}