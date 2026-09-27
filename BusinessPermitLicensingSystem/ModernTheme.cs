using BusinessPermitLicensingSystem.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem
{
    internal static class ModernPalette
    {
        internal static readonly Color Navy = Color.FromArgb(15, 38, 61);
        internal static readonly Color NavyRaised = Color.FromArgb(26, 57, 82);
        internal static readonly Color Ink = Color.FromArgb(20, 42, 58);
        internal static readonly Color Taupe = Color.FromArgb(219, 205, 190);
        internal static readonly Color Canvas = Color.FromArgb(225, 214, 202);
        internal static readonly Color Surface = Color.FromArgb(241, 233, 224);
        internal static readonly Color Field = Color.FromArgb(248, 243, 237);
        internal static readonly Color Border = Color.FromArgb(189, 171, 154);
        internal static readonly Color Muted = Color.FromArgb(105, 93, 81);
        internal static readonly Color LightText = Color.FromArgb(248, 243, 237);
        internal static readonly Color Warning = Color.FromArgb(130, 84, 70);
    }

    /// <summary>Applies the shared palette while preserving each form's layout and behavior.</summary>
    internal static class ModernTheme
    {
        private static readonly HashSet<Form> StyledForms = new();
        private static bool installed;

        internal static void Install()
        {
            if (installed) return;
            installed = true;

            Application.Idle += (_, _) =>
            {
                foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
                {
                    if (!StyledForms.Add(form)) continue;
                    form.FormClosed += (_, _) => StyledForms.Remove(form);

                    // Their custom layouts use ModernPalette directly.
                    if (form is LogInForm or AccountCreationForm) continue;

                    form.SuspendLayout();
                    form.BackColor = ModernPalette.Canvas;
                    StyleTree(form);
                    form.ResumeLayout(false);
                }
            };
        }

        private static void StyleTree(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.GetType().Namespace?.StartsWith("Microsoft.Reporting") == true)
                    continue;
                StyleControl(control);
                if (control.HasChildren) StyleTree(control);
            }

            parent.ControlAdded += (_, e) =>
            {
                if (e.Control == null ||
                    e.Control.GetType().Namespace?.StartsWith("Microsoft.Reporting") == true)
                    return;
                StyleControl(e.Control);
                if (e.Control.HasChildren) StyleTree(e.Control);
            };
        }

        private static void StyleControl(Control control)
        {
            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case DataGridView grid:
                    StyleGrid(grid);
                    break;
                case TextBox box:
                    box.BackColor = ModernPalette.Field;
                    box.ForeColor = ModernPalette.Ink;
                    if (!box.Multiline) box.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox combo:
                    combo.BackColor = ModernPalette.Field;
                    combo.ForeColor = ModernPalette.Ink;
                    combo.FlatStyle = FlatStyle.Flat;
                    break;
                case DateTimePicker picker:
                    picker.CalendarMonthBackground = ModernPalette.Field;
                    picker.CalendarForeColor = ModernPalette.Ink;
                    break;
                case GroupBox group:
                    group.BackColor = ModernPalette.Surface;
                    group.ForeColor = ModernPalette.Navy;
                    break;
                case TabPage page:
                    page.BackColor = ModernPalette.Surface;
                    break;
                case Panel panel:
                    if (panel.BackColor == SystemColors.GradientActiveCaption ||
                        panel.BackColor == Color.White ||
                        panel.BackColor == SystemColors.Control)
                        panel.BackColor = ModernPalette.Surface;
                    else if (panel.BackColor == Color.FromArgb(30, 60, 90))
                        panel.BackColor = ModernPalette.Navy;
                    break;
                case Label label:
                    bool darkParent = label.Parent?.BackColor.GetBrightness() < 0.45f;
                    if (darkParent)
                    {
                        if (label.ForeColor == Color.White ||
                            label.ForeColor == Color.LightSteelBlue ||
                            label.ForeColor == SystemColors.ControlText ||
                            label.ForeColor == Color.Black)
                            label.ForeColor = ModernPalette.LightText;
                    }
                    else if (label.ForeColor == SystemColors.ControlText ||
                             label.ForeColor == Color.Black)
                        label.ForeColor = ModernPalette.Ink;
                    break;
                case ListView list:
                    list.BackColor = ModernPalette.Surface;
                    list.ForeColor = ModernPalette.Ink;
                    break;
            }
        }

        private static void StyleButton(Button button)
        {
            ApplyButtonColors(button);
            if (button.Image is Image originalIcon)
            {
                Image icon = TintIcon(originalIcon, IconColor(button), 30);
                button.Image = icon;
                button.ImageAlign = ContentAlignment.MiddleLeft;
                button.Text = button.Text.Trim();
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Padding = new Padding(12, 0, 0, 0);
                button.Disposed += (_, _) => icon.Dispose();
                return;
            }
            AttachActionIcon(button);
        }

        private static void ApplyButtonColors(Button button)
        {
            string text = button.Text.Trim();
            bool secondary = ContainsAny(text, "Cancel", "Back", "Close", "Exit", "Log Out");
            bool destructive = ContainsAny(text, "Archive", "Delete", "Remove");
            bool primary = ContainsAny(text, "Save", "Confirm", "Generate", "Test",
                "Continue", "Create", "Add", "Import", "Export");
            bool navigation = button.Height >= 58;

            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.BackColor = secondary ? ModernPalette.Surface
                : destructive ? ModernPalette.Taupe
                : navigation ? ModernPalette.NavyRaised
                : primary ? ModernPalette.Navy : ModernPalette.Surface;
            bool dark = button.BackColor == ModernPalette.Navy ||
                        button.BackColor == ModernPalette.NavyRaised;
            button.ForeColor = dark ? ModernPalette.LightText
                : destructive ? ModernPalette.Warning : ModernPalette.Ink;
            button.FlatAppearance.BorderSize = dark ? 0 : 1;
            button.FlatAppearance.BorderColor = ModernPalette.Border;
            button.FlatAppearance.MouseOverBackColor = dark
                ? ModernPalette.Navy : ModernPalette.Taupe;
        }

        private static bool ContainsAny(string text, params string[] words) =>
            words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));

        private static Color IconColor(Button button) =>
            button.ForeColor == ModernPalette.LightText
                ? ModernPalette.Taupe : ModernPalette.Navy;

        private static void AttachActionIcon(Button button)
        {
            Image? generated = null;
            void Refresh()
            {
                ApplyButtonColors(button);
                generated?.Dispose();
                generated = null;
                Image? source = ResolveActionIcon(button.Text);
                if (source == null) { button.Image = null; return; }

                int size = button.Width < 120 || button.Height < 38 ? 17
                    : button.Width < 180 ? 22 : 26;
                generated = TintIcon(source, IconColor(button), size);
                button.Image = generated;
                button.ImageAlign = ContentAlignment.MiddleLeft;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Padding = new Padding(size < 20 ? 5 : 10, 0, 0, 0);
            }

            Refresh();
            button.TextChanged += (_, _) => Refresh();
            button.Disposed += (_, _) => generated?.Dispose();
        }

        private static Image? ResolveActionIcon(string caption)
        {
            string text = caption.Trim().ToLowerInvariant();
            if (text.Contains("stall owner")) return Properties.Resources.icons8_profile_64;
            if (text.Contains("vehicle") || text.Contains("delivery"))
                return Properties.Resources.icons8_container_truck_64;
            if (text.Contains("test connection") || text.Contains("testing"))
                return Properties.Resources.icons8_settings_64__1_;
            if (text.Contains("save") || text.Contains("confirm") ||
                text.Contains("continue"))
                return Properties.Resources.icons8_save_64;
            if (text.Contains("add") || text.Contains("create"))
                return Properties.Resources.icons8_create_64;
            if (text.Contains("cancel") || text.Contains("close") ||
                text.Contains("back") || text.Contains("exit"))
                return Properties.Resources.icons8_back_64;
            if (text.Contains("generate") || text.Contains("report"))
                return Properties.Resources.icons8_reports_64;
            if (text.Contains("history")) return Properties.Resources.icons8_payment_history_64;
            return null;
        }

        internal static Image TintIcon(Image source, Color color, int size = 24)
        {
            var icon = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(icon))
            {
                graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, size, size);
            }
            for (int y = 0; y < icon.Height; y++)
            for (int x = 0; x < icon.Width; x++)
            {
                Color pixel = icon.GetPixel(x, y);
                if (pixel.A > 0)
                    icon.SetPixel(x, y, Color.FromArgb(pixel.A, color));
            }
            return icon;
        }

        private static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = ModernPalette.Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = ModernPalette.Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ModernPalette.Navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernPalette.LightText;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ModernPalette.Navy;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = ModernPalette.LightText;
            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", grid.Font.Size, FontStyle.Bold);
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 36);
            grid.DefaultCellStyle.BackColor = ModernPalette.Surface;
            grid.DefaultCellStyle.ForeColor = ModernPalette.Ink;
            grid.DefaultCellStyle.SelectionBackColor = ModernPalette.Taupe;
            grid.DefaultCellStyle.SelectionForeColor = ModernPalette.Ink;
            grid.AlternatingRowsDefaultCellStyle.BackColor = ModernPalette.Field;
            grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 29);
        }
    }
}
