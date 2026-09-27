using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    internal sealed class VehiclePermitDetailDialog : Form
    {
        private readonly string _vin;
        private readonly int _year;
        private readonly Dictionary<string, TextBox> _amounts = new();
        private readonly TextBox[] _otherDescriptions = new TextBox[4];
        private readonly Label _totalLabel = new();
        private readonly TextBox _line = new();
        private readonly TextBox _description = new();
        private readonly TextBox _location = new();
        private readonly TextBox _employees = new();
        private readonly TextBox _capital = new();
        private readonly TextBox _stickers = new();
        private readonly ComboBox _organization = new();
        private readonly ComboBox _quarter = new();
        private readonly ComboBox _sanitaryType = new();
        private readonly ComboBox _fireType = new();
        private static readonly CultureInfo AmountCulture = CultureInfo.GetCultureInfo("en-PH");

        public VehiclePermitDetailDialog(
            int year, string company, string driver, string plate,
            string vin, string status, string orNumber,
            string lineOfBusiness, string location, string employees)
        {
            _year = year;
            _vin = vin;
            Text = $"View New Permit | {year}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1160, 800);
            MinimumSize = new Size(980, 680);
            BackColor = ModernPalette.Canvas;
            Font = new Font("Segoe UI", 10F);

            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = ModernPalette.Navy };
            header.Controls.Add(new Label
            {
                Text = $"View New Permit  •  {year}",
                Location = new Point(24, 12),
                Size = new Size(760, 38),
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = ModernPalette.LightText
            });
            header.Controls.Add(new Label
            {
                Text = $"Status: {status}" + (string.IsNullOrWhiteSpace(orNumber) ? "" : $"    OR: {orNumber}"),
                Location = new Point(26, 51),
                Size = new Size(950, 22),
                ForeColor = ModernPalette.Taupe
            });

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = ModernPalette.Surface };
            _totalLabel.Text = "GRAND TOTAL    0.00";
            _totalLabel.Dock = DockStyle.Left;
            _totalLabel.Width = 600;
            _totalLabel.Padding = new Padding(24, 0, 0, 0);
            _totalLabel.TextAlign = ContentAlignment.MiddleLeft;
            _totalLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            _totalLabel.ForeColor = ModernPalette.Navy;
            var update = MakeButton("Update", ModernPalette.Navy, ModernPalette.LightText, 130);
            var cancel = MakeButton("Cancel", ModernPalette.Taupe, ModernPalette.Navy, 130);
            SetButtonIcon(update, Properties.Resources.icons8_save_64);
            SetButtonIcon(cancel, Properties.Resources.icons8_cancel_64);
            update.Click += Update_Click;
            cancel.Click += (_, _) => Close();
            footer.Controls.Add(_totalLabel);
            footer.Controls.Add(update);
            footer.Controls.Add(cancel);
            footer.Resize += (_, _) =>
            {
                cancel.Location = new Point(footer.ClientSize.Width - 154, 18);
                update.Location = new Point(cancel.Left - 142, 18);
            };
            cancel.Location = new Point(footer.ClientSize.Width - 154, 18);
            update.Location = new Point(cancel.Left - 142, 18);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = ModernPalette.Canvas
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var basics = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(16),
                BackColor = ModernPalette.Surface,
                Margin = new Padding(0, 0, 0, 16)
            };
            basics.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            basics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            basics.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            basics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            AddBasicRow(basics, "Company", StaticValue(company), "Driver", StaticValue(driver));
            AddBasicRow(basics, "Plate number", StaticValue(plate), "VIN", StaticValue(vin));
            _line.Text = lineOfBusiness;
            _description.Text = "";
            _location.Text = location;
            _employees.Text = string.IsNullOrWhiteSpace(employees) ? "0" : employees;
            AddBasicRow(basics, "Line of business", Edit(_line), "Description", Edit(_description));
            AddBasicRow(basics, "Location", Edit(_location), "No. of employees", Edit(_employees));
            _organization.Items.AddRange(new object[] { "Individual", "Sole Proprietorship", "Partnership", "Corporation", "Cooperative", "Other" });
            _organization.DropDownStyle = ComboBoxStyle.DropDownList;
            _quarter.Items.AddRange(new object[] { "1ST QUARTER", "2ND QUARTER", "3RD QUARTER", "4TH QUARTER" });
            _quarter.DropDownStyle = ComboBoxStyle.DropDownList;
            _quarter.SelectedIndex = 0;
            AddBasicRow(basics, "Organization", Edit(_organization), "Quarter", Edit(_quarter));
            _capital.Text = "0.00";
            _stickers.Text = "0";
            AddBasicRow(basics, "Capital", Edit(_capital), "No. of stickers", Edit(_stickers));
            body.Controls.Add(basics, 0, 0);
            body.SetColumnSpan(basics, 2);

            var permitFees = CreateFeeGroup("Permit and license fees");
            AddFee(permitFees, "Mayor's Permit Fee");
            _sanitaryType.Items.AddRange(new object[] { "OTHER", "FOOD", "NON-FOOD" });
            _sanitaryType.SelectedIndex = 0;
            AddFee(permitFees, "Sanitary Fee", _sanitaryType);
            foreach (string name in new[] { "Garbage Fee", "Market Clearance", "Occupational Permit",
                         "Sticker Fee", "Tobacco License Fee", "Liquor License Fee" })
                AddFee(permitFees, name);
            _fireType.Items.AddRange(new object[] { "ESTAB", "OTHER" });
            _fireType.SelectedIndex = 0;
            AddFee(permitFees, "Fire Inspection Fee", _fireType);
            AddFee(permitFees, "Certification Fee");
            AddFee(permitFees, "Plate Fee");

            var additionalFees = CreateFeeGroup("Additional fees");
            foreach (string name in new[] { "Desktop Fee", "Videoke Fee", "Weights / Measures", "Storage Fee" })
                AddFee(additionalFees, name);
            for (int i = 0; i < 4; i++)
                AddOtherFee(additionalFees, i);
            body.Controls.Add(permitFees, 0, 1);
            body.Controls.Add(additionalFees, 1, 1);
            scroll.Controls.Add(body);

            Controls.Add(scroll);
            Controls.Add(footer);
            Controls.Add(header);
            AcceptButton = update;
            CancelButton = cancel;
            Shown += (_, _) => { if (!string.IsNullOrWhiteSpace(_vin)) LoadDraft(); };
        }

        private static Button MakeButton(string text, Color back, Color fore, int width)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, 40),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static void SetButtonIcon(Button button, Image source)
        {
            var icon = new Bitmap(source, new Size(22, 22));
            button.Image = icon;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Padding = new Padding(9, 0, 12, 0);
            button.Disposed += (_, _) => icon.Dispose();
        }

        private static Control StaticValue(string value) => new Label
        {
            Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ModernPalette.Ink,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        private static Control Edit(Control control)
        {
            control.Dock = DockStyle.Fill;
            control.BackColor = ModernPalette.Field;
            control.ForeColor = ModernPalette.Ink;
            return control;
        }

        private static void AddBasicRow(TableLayoutPanel table, string leftName, Control left,
            string rightName, Control right)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            table.Controls.Add(BasicLabel(leftName), 0, row);
            table.Controls.Add(left, 1, row);
            table.Controls.Add(BasicLabel(rightName), 2, row);
            table.Controls.Add(right, 3, row);
        }

        private static Label BasicLabel(string text) => new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ModernPalette.Muted
        };

        private static GroupBox CreateFeeGroup(string title)
        {
            var group = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = ModernPalette.Surface,
                ForeColor = ModernPalette.Navy,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 12, 0)
            };
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            group.Controls.Add(table);
            return group;
        }

        private void AddFee(GroupBox group, string name, ComboBox? selector = null)
        {
            var table = (TableLayoutPanel)group.Controls[0];
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.Controls.Add(BasicLabel(name), 0, row);
            if (selector != null) table.Controls.Add(Edit(selector), 1, row);
            var amount = MakeAmount(name);
            table.Controls.Add(amount, 2, row);

        }

        private void AddOtherFee(GroupBox group, int index)
        {
            var table = (TableLayoutPanel)group.Controls[0];
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            var description = new TextBox
            {
                PlaceholderText = index == 0 ? "Other fee description" : "Description",
                BackColor = ModernPalette.Field,
                ForeColor = ModernPalette.Ink,
                Dock = DockStyle.Fill
            };
            _otherDescriptions[index] = description;
            table.Controls.Add(description, 0, row);
            table.SetColumnSpan(description, 2);
            table.Controls.Add(MakeAmount($"Other Fee {index + 1}"), 2, row);
        }

        private TextBox MakeAmount(string name)
        {
            var box = new TextBox
            {
                Name = "amount_" + _amounts.Count,
                AccessibleName = name,
                Text = "0.00",
                Dock = DockStyle.Fill,
                TextAlign = HorizontalAlignment.Right,
                BackColor = ModernPalette.Field,
                ForeColor = ModernPalette.Ink
            };
            box.TextChanged += (_, _) => Recalculate();
            box.Leave += (_, _) =>
            {
                if (TryAmount(box, out decimal value)) box.Text = value.ToString("N2", AmountCulture);
            };
            _amounts.Add(name, box);
            return box;
        }

        private static bool TryAmount(TextBox box, out decimal amount)
        {
            return decimal.TryParse(box.Text, NumberStyles.Number, AmountCulture, out amount)
                && amount >= 0 && decimal.Round(amount, 2) == amount;
        }

        private bool TryTotal(out decimal total)
        {
            total = 0;
            bool valid = true;
            foreach (var box in _amounts.Values)
            {
                bool okay = TryAmount(box, out decimal value);
                box.BackColor = okay ? ModernPalette.Field : Color.MistyRose;
                if (!okay) { valid = false; continue; }
                try { total = checked(total + value); }
                catch (OverflowException) { valid = false; }
            }
            if (total > 9999999999999999.99m) valid = false;
            return valid;
        }

        private void Recalculate()
        {
            if (TryTotal(out decimal total))
            {
                _totalLabel.Text = $"GRAND TOTAL    {total.ToString("N2", AmountCulture)}";
                _totalLabel.ForeColor = ModernPalette.Navy;
            }
            else
            {
                _totalLabel.Text = "GRAND TOTAL    Check amounts";
                _totalLabel.ForeColor = Color.DarkRed;
            }
        }

        private void LoadDraft()
        {
            try
            {
                string? json = Database.GetVehiclePermitFeeDraft(_vin, _year);
                if (string.IsNullOrWhiteSpace(json)) return;
                var draft = JsonSerializer.Deserialize<FeeDraft>(json);
                if (draft == null) return;
                foreach (var pair in draft.Amounts)
                    if (_amounts.TryGetValue(pair.Key, out var box)) box.Text = pair.Value;
                for (int i = 0; i < _otherDescriptions.Length && i < draft.OtherDescriptions.Length; i++)
                    _otherDescriptions[i].Text = draft.OtherDescriptions[i];
                _line.Text = draft.LineOfBusiness;
                _description.Text = draft.Description;
                _location.Text = draft.Location;
                _employees.Text = draft.Employees;
                _capital.Text = draft.Capital;
                _stickers.Text = draft.Stickers;
                _organization.Text = draft.Organization;
                _quarter.Text = draft.Quarter;
                _sanitaryType.Text = draft.SanitaryType;
                _fireType.Text = draft.FireType;
                Recalculate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load permit fees: {ex.Message}", "Permit fees",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Update_Click(object? sender, EventArgs e)
        {
            if (!TryTotal(out decimal total))
            {
                MessageBox.Show("Enter valid, non-negative amounts with at most two decimal places.",
                    "Permit fees", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(_capital.Text, NumberStyles.Number, AmountCulture, out decimal capital) || capital < 0 ||
                !int.TryParse(_stickers.Text, out int stickers) || stickers < 0 ||
                !int.TryParse(_employees.Text, out int employees) || employees < 0)
            {
                MessageBox.Show("Enter valid non-negative capital, employee, and sticker values.",
                    "Permit details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var draft = new FeeDraft
            {
                Amounts = _amounts.ToDictionary(x => x.Key, x => x.Value.Text),
                OtherDescriptions = _otherDescriptions.Select(x => x.Text.Trim()).ToArray(),
                LineOfBusiness = _line.Text.Trim(),
                Description = _description.Text.Trim(),
                Location = _location.Text.Trim(),
                Employees = employees.ToString(),
                Capital = capital.ToString("N2", AmountCulture),
                Stickers = stickers.ToString(),
                Organization = _organization.Text,
                Quarter = _quarter.Text,
                SanitaryType = _sanitaryType.Text,
                FireType = _fireType.Text
            };
            string json = JsonSerializer.Serialize(draft);
            var result = Database.SaveVehiclePermitFeeDraft(_vin, _year, json, total);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage ?? "Could not save permit fees.",
                    "Permit fees", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class FeeDraft
        {
            public Dictionary<string, string> Amounts { get; set; } = new();
            public string[] OtherDescriptions { get; set; } = Array.Empty<string>();
            public string LineOfBusiness { get; set; } = "";
            public string Description { get; set; } = "";
            public string Location { get; set; } = "";
            public string Employees { get; set; } = "0";
            public string Capital { get; set; } = "0.00";
            public string Stickers { get; set; } = "0";
            public string Organization { get; set; } = "";
            public string Quarter { get; set; } = "";
            public string SanitaryType { get; set; } = "";
            public string FireType { get; set; } = "";
        }
    }
}
