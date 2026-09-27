using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class VehicleProfiling
    {
        private int _permitYear;
        private string _permitStatus = "Unpaid";
        private TabControl annualTabs = null!;
        private DataGridView renewalGrid = null!;
        private DataGridView oldGrid = null!;

        private void InitializeAnnualHistory()
        {
            ClientSize = new Size(1236, 850);
            MinimumSize = new Size(1252, 889);
            btnSave.Location = new Point(908, 765);
            btnCancel.Location = new Point(1074, 765);

            annualTabs = new TabControl
            {
                Name = "annualTabs",
                Location = new Point(10, 458),
                Size = new Size(1140, 292),
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var renewPage = new TabPage("Renew") { Name = "renewPage" };
            var oldPage = new TabPage("Old") { Name = "oldPage" };
            renewalGrid = CreateYearGrid("renewalGrid");
            oldGrid = CreateYearGrid("oldGrid");
            renewPage.Controls.Add(renewalGrid);
            oldPage.Controls.Add(oldGrid);
            annualTabs.TabPages.Add(renewPage);
            annualTabs.TabPages.Add(oldPage);
            Controls.Add(annualTabs);

            renewalGrid.CellClick += YearGrid_CellClick;
            oldGrid.CellClick += YearGrid_CellClick;
            annualTabs.Visible = false;
        }

        private static DataGridView CreateYearGrid(string name)
        {
            return new DataGridView
            {
                Name = name,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ModernPalette.Surface,
                BorderStyle = BorderStyle.None
            };
        }

        private void LoadAnnualHistory()
        {
            annualTabs.Visible = true;
            DataTable history;
            try
            {
                history = Database.GetVehiclePermitHistory(_editVIN);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load annual payment history: {ex.Message}",
                    "Vehicle permit history", MessageBoxButtons.OK, MessageBoxIcon.Error);
                history = new DataTable();
            }

            var renew = CreateYearTable();
            var old = CreateYearTable();
            int currentYear = Math.Max(DateTime.Today.Year, _permitYear);
            DataRow? currentPayment = null;
            foreach (DataRow payment in history.Rows)
            {
                if (int.TryParse(payment["Permit Year"]?.ToString(), out int year) && year == currentYear)
                {
                    if (currentPayment == null) currentPayment = payment;
                    else AddHistoryRow(old, payment);
                }
                else
                {
                    AddHistoryRow(old, payment);
                }
            }

            if (currentPayment != null)
            {
                AddHistoryRow(renew, currentPayment);
            }
            else
            {
                renew.Rows.Add(currentYear, _permitYear == currentYear ? _permitStatus : "Unpaid", 0m, "", "");
            }

            renewalGrid.DataSource = renew;
            oldGrid.DataSource = old;
            FormatYearGrid(renewalGrid);
            FormatYearGrid(oldGrid);
        }

        private static DataTable CreateYearTable()
        {
            var table = new DataTable();
            table.Columns.Add("Permit Year", typeof(int));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Amount Paid", typeof(decimal));
            table.Columns.Add("OR Number", typeof(string));
            table.Columns.Add("Date Paid", typeof(string));
            return table;
        }

        private static void AddHistoryRow(DataTable target, DataRow payment)
        {
            target.Rows.Add(
                Convert.ToInt32(payment["Permit Year"], CultureInfo.InvariantCulture),
                "Paid",
                Convert.ToDecimal(payment["Amount Paid"], CultureInfo.InvariantCulture),
                payment["OR Number"]?.ToString() ?? "",
                payment["Date Paid"]?.ToString() ?? "");
        }

        private static void FormatYearGrid(DataGridView grid)
        {
            if (grid.Columns["Amount Paid"] != null)
                grid.Columns["Amount Paid"]!.DefaultCellStyle.Format = "N2";
            grid.ClearSelection();
        }

        private void YearGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;
            var row = grid.Rows[e.RowIndex];
            if (!int.TryParse(row.Cells["Permit Year"].Value?.ToString(), out int year)) return;
            using var detail = new VehiclePermitDetailDialog(
                year, txtCompanyName.Text, txtDriverName.Text,
                txtPlateNumber.Text, txtVIN.Text,
                row.Cells["Status"].Value?.ToString() ?? "Unpaid",
                row.Cells["OR Number"].Value?.ToString() ?? "", txtLine.Text, txtLoc.Text, textBox1.Text);
            detail.ShowDialog(this);
        }
    }
}
