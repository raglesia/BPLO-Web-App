using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class AccountCreationForm
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

            AutoScaleMode = AutoScaleMode.None;
            var workingArea = Screen.FromControl(this).WorkingArea;
            int windowWidth = Math.Min(1100, workingArea.Width - 30);
            int windowHeight = Math.Min(790, workingArea.Height - 50);
            ClientSize = new Size(windowWidth, windowHeight);
            BackColor = ModernPalette.Surface;
            Font = new Font("Segoe UI", 10F);
            Text = "Masinloc BPLS | Create account";

            var brand = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(380, windowHeight),
                BackColor = Navy
            };
            Controls.Add(brand);
            brand.Controls.Add(new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(7, windowHeight),
                BackColor = Taupe
            });

            var logoPath = Path.Combine(Application.StartupPath, "Resources", "Masinloc Logo.jpg");
            if (File.Exists(logoPath))
            {
                brand.Controls.Add(new PictureBox
                {
                    Location = new Point(42, 52),
                    Size = new Size(78, 78),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(logoPath),
                    BackColor = ModernPalette.Surface
                });
            }

            brand.Controls.Add(MakeLabel("MUNICIPALITY OF MASINLOC", 42, 168, 270, 26,
                10, FontStyle.Bold, Taupe));
            brand.Controls.Add(MakeLabel("Business Permit\nLicensing System", 42, 207, 300, 132,
                24, FontStyle.Bold, ModernPalette.LightText));
            brand.Controls.Add(new Panel
            {
                Location = new Point(42, 337),
                Size = new Size(46, 3),
                BackColor = Taupe
            });
            brand.Controls.Add(MakeLabel("Create an account to access\nyour municipal workspace.", 42, 365, 270, 64,
                11, FontStyle.Regular, Taupe));
            brand.Controls.Add(MakeLabel("MASINLOC  •  ZAMBALES", 42, windowHeight - 50, 300, 24,
                9, FontStyle.Regular, ModernPalette.Border));

            panel1.Location = new Point(380, 0);
            panel1.Size = new Size(windowWidth - 380, windowHeight);
            panel1.AutoScroll = true;
            panel1.AutoScrollMinSize = new Size(700, 750);
            panel1.BackColor = ModernPalette.Surface;

            panel1.Controls.Add(MakeLabel("Create your account", 100, 38, 520, 50,
                25, FontStyle.Bold, Ink));
            panel1.Controls.Add(MakeLabel("Enter your details to get started.", 102, 87, 520, 28,
                10, FontStyle.Regular, Muted));

            AddField(panel1, label1, txtFullName, "Full name", "Enter your full name", 124);
            AddField(panel1, label5, txtPosition, "Position / title", "Enter your position or title", 214);
            AddField(panel1, label2, txtuname, "Username", "Choose a username", 304);
            AddField(panel1, label3, txtpass, "Password", "Create a password", 394);
            AddField(panel1, label4, txtconpass, "Confirm password", "Re-enter your password", 484);

            var showPassword = new Button
            {
                Text = "Show passwords",
                Location = new Point(100, 575),
                Size = new Size(145, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernPalette.Surface,
                ForeColor = Navy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabIndex = 5
            };
            showPassword.FlatAppearance.BorderSize = 0;
            showPassword.Click += (_, _) =>
            {
                bool hidden = !txtpass.UseSystemPasswordChar;
                txtpass.UseSystemPasswordChar = hidden;
                txtconpass.UseSystemPasswordChar = hidden;
                showPassword.Text = hidden ? "Show passwords" : "Hide passwords";
            };
            panel1.Controls.Add(showPassword);

            StyleButton(btnCreate, "Create account", 100, 620, 520, 50, Navy, ModernPalette.LightText);
            btnCreate.TabIndex = 6;
            StyleButton(btnCancel, "Back to sign in", 100, 680, 520, 40, ModernPalette.Taupe, Navy);
            btnCancel.TabIndex = 7;

            AcceptButton = btnCreate;
            CancelButton = btnCancel;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        private static void AddField(Panel parent, Label label, TextBox input,
            string caption, string placeholder, int y)
        {
            label.Text = caption;
            label.Location = new Point(100, y);
            label.AutoSize = false;
            label.Size = new Size(520, 26);
            label.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label.ForeColor = Ink;

            parent.Controls.Add(input);
            input.Location = new Point(100, y + 30);
            input.Size = new Size(520, 39);
            input.BorderStyle = BorderStyle.FixedSingle;
            input.BackColor = Field;
            input.ForeColor = Ink;
            input.Font = new Font("Segoe UI", 12F);
            input.PlaceholderText = placeholder;
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
