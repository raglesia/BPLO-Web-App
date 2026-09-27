using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class PaymentHistoryForm : Form
    {
        private void InitializeComponent() { }

        private DataGridView dgvHistory;
        private Label lblSummary;

        private static readonly CultureInfo PhCulture = new("en-PH");

        public PaymentHistoryForm(
            string sin,
            string fullName,
            string businessName,
            DataTable history)
        {
            SetupForm();
            SetupLabels(sin, fullName, businessName);
            SetupGrid(history);
            SetupSummary(history);
            SetupCloseButton();
        }

        private void SetupForm()
        {
            Text = "Masinloc - BPLS";
            Size = new Size(1650, 610);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Font = new Font("Segoe UI", 10);

            Icon = new Icon(Path.Combine(
                Application.StartupPath,
                "Resources",
                "MasinlocLogoIcon.ico"));
        }

        private void SetupLabels(
            string sin,
            string fullName,
            string businessName)
        {
            Controls.Add(new Label
            {
                Text = $"Payment History — {businessName}",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(15, 15),
                AutoSize = true
            });

            Controls.Add(new Label
            {
                Text = $"{fullName}  |  {sin}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(15, 45),
                AutoSize = true
            });
        }

        private void SetupGrid(DataTable history)
        {
            dgvHistory = new DataGridView
            {
                Location = new Point(15, 70),
                Size = new Size(1580, 430),
                DataSource = history,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9)
            };

            typeof(DataGridView)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(dgvHistory, true);

            dgvHistory.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            dgvHistory.RowsDefaultCellStyle.BackColor = Color.White;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

            dgvHistory.DataBindingComplete += (s, e) =>
            {
                FormatCurrencyColumns();
                SetColumnWidths();

                foreach (DataGridViewColumn col in dgvHistory.Columns)
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
            };

            Controls.Add(dgvHistory);
        }

        private void FormatCurrencyColumns()
        {
            string[] moneyColumns =
            {
                "Rent Paid",
                "Additional Charges",
                "Penalty",
                "Amount Paid"
            };

            foreach (string name in moneyColumns)
            {
                if (dgvHistory.Columns[name] == null)
                    continue;

                dgvHistory.Columns[name].DefaultCellStyle.Format = "C2";
                dgvHistory.Columns[name].DefaultCellStyle.FormatProvider = PhCulture;
            }
        }

        private void SetColumnWidths()
        {
            SetFillWeight("OR Number", 70);
            SetFillWeight("Billing Period", 170);
            SetFillWeight("Rent Paid", 90);
            SetFillWeight("Additional Charges", 105);
            SetFillWeight("Penalty", 85);
            SetFillWeight("Amount Paid", 95);
            SetFillWeight("Date Paid", 110);
            SetFillWeight("Recorded By", 100);
        }

        private void SetFillWeight(
            string columnName,
            float weight)
        {
            if (dgvHistory.Columns[columnName] != null)
                dgvHistory.Columns[columnName].FillWeight = weight;
        }

        private void SetupSummary(DataTable history)
        {
            decimal totalPaid = 0;
            decimal totalPenalty = 0;

            foreach (DataRow row in history.Rows)
            {
                totalPaid += Convert.ToDecimal(row["Amount Paid"]);
                totalPenalty += Convert.ToDecimal(row["Penalty"]);
            }

            lblSummary = new Label
            {
                Text =
                    $"Total Payments: {history.Rows.Count}     " +
                    $"Total Amount Paid: {totalPaid.ToString("C2", PhCulture)}     " +
                    $"Total Penalty: {totalPenalty.ToString("C2", PhCulture)}",

                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(10, 515),
                AutoSize = true
            };

            Controls.Add(lblSummary);
        }

        private void SetupCloseButton()
        {
            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(1540, 510),
                Size = new Size(85, 32),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            Controls.Add(btnClose);
        }
    }
}