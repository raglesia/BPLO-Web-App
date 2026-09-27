using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class ORNumberDialog : Form
    {
        // ===================== PROPERTIES ===================== //
        public string ORNumber { get; private set; } = "";

        // ===================== CONTROLS ===================== //
        private TextBox txtORNumber;

        // ===================== CONSTRUCTOR ===================== //
        public ORNumberDialog()
        {
            InitializeComponent();
            SetupUI();
        }

        // ===================== UI SETUP ===================== //
        private void SetupUI()
        {
            Text = "OR Number Required";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Size = new Size(450, 210);
            BackColor = Color.White;
            Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));

            var lbl = new Label
            {
                Text = "Enter Official Receipt (OR) Number:",
                Location = new Point(30, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            Controls.Add(lbl);

            txtORNumber = new TextBox
            {
                Location = new Point(30, 50),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 10),
                MaxLength = 50
            };
            Controls.Add(txtORNumber);

            var btnConfirm = new Button
            {
                Text = "Confirm",
                Location = new Point(195, 100),
                Size = new Size(110, 34),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            btnConfirm.Click += BtnConfirm_Click;
            Controls.Add(btnConfirm);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(310, 100),
                Size = new Size(110, 34),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            btnCancel.Click += BtnCancel_Click;
            Controls.Add(btnCancel);

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;
        }

        // ===================== EVENTS ===================== //
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtORNumber.Text))
            {
                MessageBox.Show(
                    "Please enter the OR Number.",
                    "Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtORNumber.Focus();
                return;
            }

            string orNumber = txtORNumber.Text.Trim();

            if (Database.ORNumberExists(orNumber))
            {
                MessageBox.Show(
                    $"OR Number '{orNumber}' already exists.\n\n" +
                    "Please check the physical receipt and enter the correct OR Number.",
                    "Duplicate OR Number",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtORNumber.Clear();
                txtORNumber.Focus();
                return;
            }

            ORNumber = orNumber;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}