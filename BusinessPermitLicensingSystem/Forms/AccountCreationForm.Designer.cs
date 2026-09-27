namespace BusinessPermitLicensingSystem.Forms
{
    partial class AccountCreationForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AccountCreationForm));
            txtFullName = new TextBox();
            txtuname = new TextBox();
            txtpass = new TextBox();
            txtconpass = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            btnCreate = new Button();
            btnCancel = new Button();
            txtPosition = new TextBox();
            label5 = new Label();
            toolTip1 = new ToolTip(components);
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtFullName
            // 
            txtFullName.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtFullName.Location = new Point(219, 13);
            txtFullName.Name = "txtFullName";
            txtFullName.Size = new Size(341, 39);
            txtFullName.TabIndex = 0;
            // 
            // txtuname
            // 
            txtuname.Font = new Font("Segoe UI", 12F);
            txtuname.Location = new Point(219, 110);
            txtuname.Name = "txtuname";
            txtuname.Size = new Size(341, 39);
            txtuname.TabIndex = 2;
            // 
            // txtpass
            // 
            txtpass.Font = new Font("Segoe UI", 12F);
            txtpass.Location = new Point(219, 159);
            txtpass.Name = "txtpass";
            txtpass.Size = new Size(341, 39);
            txtpass.TabIndex = 3;
            txtpass.UseSystemPasswordChar = true;
            // 
            // txtconpass
            // 
            txtconpass.Font = new Font("Segoe UI", 12F);
            txtconpass.Location = new Point(219, 204);
            txtconpass.Name = "txtconpass";
            txtconpass.Size = new Size(341, 39);
            txtconpass.TabIndex = 4;
            txtconpass.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label1.Location = new Point(16, 13);
            label1.Name = "label1";
            label1.Size = new Size(119, 30);
            label1.TabIndex = 4;
            label1.Text = "Full Name:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label2.Location = new Point(16, 115);
            label2.Name = "label2";
            label2.Size = new Size(123, 30);
            label2.TabIndex = 5;
            label2.Text = "Username: ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label3.Location = new Point(16, 164);
            label3.Name = "label3";
            label3.Size = new Size(116, 30);
            label3.TabIndex = 6;
            label3.Text = "Password: ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label4.Location = new Point(16, 209);
            label4.Name = "label4";
            label4.Size = new Size(197, 30);
            label4.TabIndex = 7;
            label4.Text = "Confirm Password:";
            // 
            // btnCreate
            // 
            btnCreate.BackColor = SystemColors.GradientActiveCaption;
            btnCreate.BackgroundImageLayout = ImageLayout.Center;
            btnCreate.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCreate.Image = Properties.Resources.icons8_create_64;
            btnCreate.ImageAlign = ContentAlignment.MiddleLeft;
            btnCreate.Location = new Point(44, 255);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new Size(223, 73);
            btnCreate.TabIndex = 5;
            btnCreate.Text = "           Create Account";
            toolTip1.SetToolTip(btnCreate, "Create Account");
            btnCreate.UseVisualStyleBackColor = false;
            btnCreate.Click += btnCreate_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = SystemColors.GradientActiveCaption;
            btnCancel.BackgroundImageLayout = ImageLayout.Center;
            btnCancel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.Image = Properties.Resources.icons8_cancel_64;
            btnCancel.ImageAlign = ContentAlignment.MiddleLeft;
            btnCancel.Location = new Point(300, 255);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(223, 73);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "     Cancel";
            toolTip1.SetToolTip(btnCancel, "Cancel");
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtPosition
            // 
            txtPosition.Font = new Font("Segoe UI", 12F);
            txtPosition.Location = new Point(219, 62);
            txtPosition.Name = "txtPosition";
            txtPosition.Size = new Size(341, 39);
            txtPosition.TabIndex = 1;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label5.Location = new Point(16, 67);
            label5.Name = "label5";
            label5.Size = new Size(163, 30);
            label5.TabIndex = 12;
            label5.Text = "Position / Title:";
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 3000;
            toolTip1.InitialDelay = 500;
            toolTip1.IsBalloon = true;
            toolTip1.ReshowDelay = 200;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(txtFullName);
            panel1.Controls.Add(txtuname);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(txtpass);
            panel1.Controls.Add(txtPosition);
            panel1.Controls.Add(txtconpass);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(btnCreate);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label4);
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(564, 348);
            panel1.TabIndex = 13;
            // 
            // AccountCreationForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientActiveCaption;
            BackgroundImageLayout = ImageLayout.None;
            ClientSize = new Size(583, 372);
            Controls.Add(panel1);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "AccountCreationForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Masinloc BPLS - Account Creation";
            Load += AccountCreationForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtFullName;
        private TextBox txtuname;
        private TextBox txtpass;
        private TextBox txtconpass;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Button btnCreate;
        private Button btnCancel;
        private TextBox txtPosition;
        private Label label5;
        private ToolTip toolTip1;
        private Panel panel1;
    }
}