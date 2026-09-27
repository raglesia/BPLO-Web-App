using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class LogInForm : Form
    {
        // ===================== CONSTRUCTOR ===================== //
        public LogInForm()
        {
            InitializeComponent();
            ApplyModernLayout();
        }

        // ===================== FORM LOAD ===================== //
        private void LogInForm_Load(object sender, EventArgs e)
        {
            MaximizeBox = false;
            txtUser.Focus();
            SetupSecurity();

            this.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));
        }


        // ===================== LOGIN ===================== //
        private void btnLogIn_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter both username and password.",
                    "Log In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var (isValid, messageOrUserId) = Database.VerifyLogin(username, password);

            if (isValid)
            {
                long userId = long.Parse(messageOrUserId!);

                Session.CurrentUserId = userId;
                Session.CurrentUsername = username;
                Session.CurrentFullName = Database.GetFullName(userId);
                Session.CurrentPosition = Database.GetPosition(userId);

                Database.LogAudit("Login", null, userId,
                    $"User '{username}' logged in.");

                new DashboardForm().Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show(
                    messageOrUserId ?? "Invalid username or password.",
                    "Log In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                txtPass.Clear();
                txtPass.Focus();
            }
        }

        // ===================== NAVIGATION ===================== //
        private void btnCreate_Click_1(object sender, EventArgs e)
        {
            new AccountCreationForm().Show();
            this.Hide();
        }

        // ===================== EXIT ===================== //
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ===================== SECURITY ===================== //
        private void SetupSecurity()
        {
            TextBox[] protectedFields = { txtUser, txtPass };

            foreach (var field in protectedFields)
            {
                field.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.V) e.SuppressKeyPress = true; };
                field.ContextMenuStrip = new ContextMenuStrip();
            }
        }

        // ===================== WINDOW SETTINGS ===================== //
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_NOCLOSE = 0x200;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;
                return cp;
            }
        }
    }
}
