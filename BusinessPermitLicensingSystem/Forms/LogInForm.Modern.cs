using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class LogInForm
    {
        private static readonly Color Ink = ModernPalette.Ink;
        private static readonly Color Muted = ModernPalette.Muted;
        private static readonly Color Navy = ModernPalette.Navy;
        private static readonly Color Taupe = ModernPalette.Taupe;
        private static readonly Color Field = ModernPalette.Field;

        private void ApplyModernLayout()
        {
            SuspendLayout();
            panel1.SuspendLayout();

            ClientSize = new Size(940, 640);
            MinimumSize = new Size(940, 640);
            MaximumSize = new Size(940, 640);
            BackColor = ModernPalette.Surface;
            Font = new Font("Segoe UI", 10F);
            Text = "Masinloc BPLS | Sign in";

            var brand = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(370, 640),
                BackColor = Navy
            };
            Controls.Add(brand);
            brand.Controls.Add(new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(7, 640),
                BackColor = Taupe
            });

            var logoPath = Path.Combine(Application.StartupPath, "Resources", "Masinloc-Logo-HD.ico");
            if (File.Exists(logoPath))
            {
                var logo = new PictureBox
                {
                    Location = new Point(42, 44),
                    Size = new Size(78, 78),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(logoPath),
                    BackColor = ModernPalette.Surface
                };
                brand.Controls.Add(logo);
            }

            brand.Controls.Add(MakeLabel("MUNICIPALITY OF MASINLOC", 42, 157, 290, 26,
                10, FontStyle.Bold, Taupe));
            brand.Controls.Add(MakeLabel("Business Permit\nLicensing System", 42, 196, 300, 112,
                26, FontStyle.Bold, ModernPalette.LightText));
            brand.Controls.Add(new Panel
            {
                Location = new Point(42, 326),
                Size = new Size(46, 3),
                BackColor = Taupe
            });
            brand.Controls.Add(MakeLabel("A secure place to manage your\nmunicipal permit records.", 42, 352, 290, 60,
                11, FontStyle.Regular, Taupe));
            brand.Controls.Add(MakeLabel("MASINLOC  •  ZAMBALES", 42, 513, 290, 22,
                9, FontStyle.Regular, ModernPalette.Border));

            panel1.Location = new Point(370, 0);
            panel1.Size = new Size(570, 640);
            panel1.BackColor = ModernPalette.Surface;

            panel1.Controls.Add(MakeLabel("Welcome back", 62, 66, 440, 50,
                25, FontStyle.Bold, Ink));
            panel1.Controls.Add(MakeLabel("Sign in to continue to your workspace.", 64, 119, 440, 27,
                10, FontStyle.Regular, Muted));

            label1.Text = "Username";
            StyleFieldLabel(label1, 64, 174);
            label2.Text = "Password";
            StyleFieldLabel(label2, 64, 264);

            var userBox = MakeInputBox(64, 204);
            var passBox = MakeInputBox(64, 294);
            panel1.Controls.Add(userBox);
            panel1.Controls.Add(passBox);

            userBox.Controls.Add(txtUser);
            txtUser.Location = new Point(17, 14);
            txtUser.Size = new Size(398, 30);
            StyleTextBox(txtUser);
            txtUser.PlaceholderText = "Enter your username";

            passBox.Controls.Add(txtPass);
            txtPass.Location = new Point(17, 14);
            txtPass.Size = new Size(345, 30);
            StyleTextBox(txtPass);
            txtPass.PlaceholderText = "Enter your password";

            var showPassword = new Button
            {
                Text = "Show",
                Location = new Point(370, 10),
                Size = new Size(66, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Field,
                ForeColor = Navy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabIndex = 3
            };
            showPassword.FlatAppearance.BorderSize = 0;
            showPassword.Click += (_, _) =>
            {
                txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
                showPassword.Text = txtPass.UseSystemPasswordChar ? "Show" : "Hide";
            };
            passBox.Controls.Add(showPassword);

            StyleButton(btnLogIn, "Sign in", 64, 383, 442, 50, Navy, ModernPalette.LightText);
            btnLogIn.TabIndex = 4;
            StyleButton(btnCreate, "Create an account", 64, 448, 235, 42, ModernPalette.Taupe, Navy);
            btnCreate.FlatAppearance.BorderColor = ModernPalette.Border;
            btnCreate.FlatAppearance.BorderSize = 1;
            btnCreate.TabIndex = 5;
            StyleButton(btnExit, "Exit", 400, 448, 106, 42, ModernPalette.Surface, Muted);
            btnExit.TabIndex = 6;

            AcceptButton = btnLogIn;
            CancelButton = btnExit;

            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        private static Label MakeLabel(string text, int x, int y, int width, int height,
            float size, FontStyle style, Color color) =>
            new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Segoe UI", size, style),
                ForeColor = color,
                BackColor = Color.Transparent
            };

        private static Panel MakeInputBox(int x, int y) =>
            new Panel
            {
                Location = new Point(x, y),
                Size = new Size(442, 56),
                BackColor = Field,
                BorderStyle = BorderStyle.FixedSingle
            };

        private static void StyleFieldLabel(Label label, int x, int y)
        {
            label.Location = new Point(x, y);
            label.AutoSize = false;
            label.Size = new Size(300, 26);
            label.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label.ForeColor = Ink;
        }

        private static void StyleTextBox(TextBox box)
        {
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Field;
            box.ForeColor = Ink;
            box.Font = new Font("Segoe UI", 12F);
        }

        private static void StyleButton(Button button, string text, int x, int y,
            int width, int height, Color background, Color foreground)
        {
            button.Text = text;
            if (button.Image is Image originalIcon)
            {
                Image icon = ModernTheme.TintIcon(originalIcon, foreground);
                button.Image = icon;
                button.ImageAlign = ContentAlignment.MiddleLeft;
                button.Padding = new Padding(12, 0, 0, 0);
                button.Disposed += (_, _) => icon.Dispose();
            }
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
    }
}
