using Microsoft.Data.SqlClient;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BusinessPermitLicensingSystem.Forms
{
    public class ConnectionSetupForm : Form
    {
        // ===================== FIELDS ===================== //
        private TextBox txtServerIP;
        private TextBox txtDatabase;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnTest;
        private Button btnSave;
        private Label lblStatus;

        // ===================== CONSTRUCTOR ===================== //
        public ConnectionSetupForm()
        {
            SetupForm();
            SetupHeader();
            SetupFields();
            SetupButtons();
            PreFillDefaults();
        }

        // ===================== FORM ===================== //
        private void SetupForm()
        {
            this.Text = "Masinloc BPLS — Database Setup";
            this.Size = new Size(480, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Font = new Font("Segoe UI", 10);
            this.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));
        }

        // ===================== HEADER ===================== //
        private void SetupHeader()
        {
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.FromArgb(30, 60, 90)
            };
            this.Controls.Add(pnlHeader);

            pnlHeader.Controls.Add(new Label
            {
                Text = "Database Connection Setup",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            });

            pnlHeader.Controls.Add(new Label
            {
                Text = "Enter your SQL Server connection details below.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightSteelBlue,
                Location = new Point(15, 42),
                AutoSize = true
            });
        }

        // ===================== FIELDS ===================== //
        private void SetupFields()
        {
            AddLabel("Server IP Address:", 20, 95);
            txtServerIP = AddTextBox(20, 117);

            this.Controls.Add(new Label
            {
                Text = "Use 'localhost' if the database is on this computer.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(20, 147),
                AutoSize = true
            });
            AddLabel("Database Name:", 20, 170);
            txtDatabase = AddTextBox(20, 192);

            AddLabel("Username:", 20, 235);
            txtUsername = AddTextBox(20, 257);

            AddLabel("Password:", 20, 300);
            txtPassword = AddTextBox(20, 322, isPassword: true);

            lblStatus = new Label
            {
                Location = new Point(20, 372),
                Size = new Size(420, 40),
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Text = ""
            };
            this.Controls.Add(lblStatus);
        }

        private void AddLabel(string text, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private TextBox AddTextBox(int x, int y, bool isPassword = false)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(420, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = isPassword
            };
            this.Controls.Add(txt);
            return txt;
        }

        // ===================== BUTTONS ===================== //
        private void SetupButtons()
        {
            // Test Connection — y=420
            btnTest = new Button
            {
                Text = "Test Connection",
                Location = new Point(20, 422),
                Size = new Size(190, 40),
                BackColor = Color.FromArgb(40, 130, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += btnTest_Click;
            this.Controls.Add(btnTest);

            // Save & Continue — y=420
            btnSave = new Button
            {
                Text = "Save & Continue →",
                Location = new Point(250, 422),
                Size = new Size(190, 40),
                BackColor = Color.FromArgb(30, 60, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += btnSave_Click;
            this.Controls.Add(btnSave);
        }

        // ===================== PRE-FILL ===================== //
        private void PreFillDefaults()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["BPLS"]?.ConnectionString ?? "";
                var builder = new SqlConnectionStringBuilder(cs);

                txtServerIP.Text = builder.DataSource;
                txtDatabase.Text = builder.InitialCatalog;
                txtUsername.Text = builder.UserID;
                txtPassword.Text = builder.Password;
            }
            catch
            {
                txtServerIP.Text = "";
                txtDatabase.Text = "Masinloc_BPLS";
                txtUsername.Text = "sa";
                txtPassword.Text = "";
            }
        }

        // ===================== TEST CONNECTION ===================== //
        private async void btnTest_Click(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;

            btnTest.Enabled = false;
            btnTest.Text = "Testing...";
            SetStatus("Testing connection...", Color.Gray);

            try
            {
                string connStr = BuildConnectionString();

                await Task.Run(() =>
                {
                    using var con = new SqlConnection(connStr);
                    con.Open();
                });

                SetStatus("✅  Connection successful!", Color.DarkGreen);
                btnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                SetStatus($"❌  {ex.Message}", Color.DarkRed);
                btnSave.Enabled = false;
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test Connection";
            }
        }

        // ===================== SAVE ===================== //
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;

            try
            {
                WriteToAppConfig(BuildConnectionString());

                MessageBox.Show(
                    "Connection settings saved!\nThe application will now continue to Login.",
                    "Setup Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save settings:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ===================== HELPERS ===================== //
        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                MessageBox.Show("Server IP is required.", "Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServerIP.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDatabase.Text))
            {
                MessageBox.Show("Database name is required.", "Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDatabase.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username is required.", "Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            return true;
        }

        private string BuildConnectionString() =>
            $"Server={txtServerIP.Text.Trim()};" +
            $"Database={txtDatabase.Text.Trim()};" +
            $"User Id={txtUsername.Text.Trim()};" +
            $"Password={txtPassword.Text};" +
            $"TrustServerCertificate=True;" +
            $"Connect Timeout=5;";

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            Application.DoEvents();
        }

        private static void WriteToAppConfig(string connectionString)
        {
            string baseDir = AppContext.BaseDirectory;
            string configPath = Path.Combine(baseDir, "BusinessPermitLicensingSystem.dll.config");

            if (!File.Exists(configPath))
                configPath = Path.Combine(baseDir, "BusinessPermitLicensingSystem.exe.config");

            if (!File.Exists(configPath))
            {
                var configs = Directory.GetFiles(baseDir, "*.config");
                if (configs.Length == 0)
                    throw new Exception($"No config file found in:\n{baseDir}");
                configPath = configs[0];
            }

            var xml = new XmlDocument();
            xml.Load(configPath);

            var node = xml.SelectSingleNode("//connectionStrings/add[@name='BPLS']");

            if (node != null)
            {
                node.Attributes!["connectionString"]!.Value = connectionString;
            }
            else
            {
                var csNode = xml.SelectSingleNode("//connectionStrings");

                if (csNode == null)
                {
                    var config = xml.SelectSingleNode("//configuration")!;
                    csNode = config.AppendChild(xml.CreateElement("connectionStrings"))!;
                }

                var newNode = xml.CreateElement("add");
                newNode.SetAttribute("name", "BPLS");
                newNode.SetAttribute("connectionString", connectionString);
                newNode.SetAttribute("providerName", "System.Data.SqlClient");
                csNode.AppendChild(newNode);
            }

            xml.Save(configPath);
            ConfigurationManager.RefreshSection("connectionStrings");
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