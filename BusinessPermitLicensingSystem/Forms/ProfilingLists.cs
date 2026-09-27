using BusinessPermitLicensingSystem.Forms;
using BusinessPermitLicensingSystem.Models;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem
{
    public partial class ProfilingLists : Form
    {
        // ===================== FIELDS ===================== //
        private DataTable dtProfiles = new DataTable();
        private bool _sortAscending = true;
        private int _lastSortedCol = -1;

        // ===================== CONSTRUCTOR ===================== //
        public ProfilingLists()
        {
            InitializeComponent();
            SetupGrid();
            SetupFilters();
            LoadProfiles();
        }

        // ===================== FORM LOAD ===================== //
        private void ProfilingLists_Load(object sender, EventArgs e)
        {
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";
            button2.Focus();

            this.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));
        }

        // ===================== SETUP ===================== //
        private void SetupGrid()
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .SetValue(dataGridView1, true);

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = true;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;

            // ✅ Add checkbox column before data binding
            var chkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "✓",
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = false
            };
            dataGridView1.Columns.Add(chkColumn);

            dataGridView1.DataBindingComplete += (s, e) =>
            {
                dataGridView1.Columns["Additional Charge"].HeaderText = "Additional Charges";

                dataGridView1.Columns["Select"].DisplayIndex = 0;
                dataGridView1.Columns["SIN"].DisplayIndex = 1;
                dataGridView1.Columns["Full Name"].DisplayIndex = 2;
                dataGridView1.Columns["Business Name"].DisplayIndex = 3;
                dataGridView1.Columns["Business Section"].DisplayIndex = 4;
                dataGridView1.Columns["Stall Number"].DisplayIndex = 5;
                dataGridView1.Columns["Stall Size"].DisplayIndex = 6;
                dataGridView1.Columns["Date of Occupancy"].DisplayIndex = 7;
                dataGridView1.Columns["Monthly Rental"].DisplayIndex = 8;
                dataGridView1.Columns["Penalty"].DisplayIndex = 9;
                dataGridView1.Columns["Additional Charge"].DisplayIndex = 10;
                dataGridView1.Columns["Payment Status"].DisplayIndex = 11;

                dataGridView1.Columns["Business Section"].Visible = false;
                dataGridView1.Columns["Stall Number"].Visible = false;
                dataGridView1.Columns["Stall Size"].Visible = false;
                dataGridView1.Columns["Date of Occupancy"].Visible = false;

                dataGridView1.Columns["Select"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["Select"].Width = 40;
                dataGridView1.Columns["SIN"].FillWeight = 80;
                dataGridView1.Columns["Full Name"].FillWeight = 150;
                dataGridView1.Columns["Business Name"].FillWeight = 200;
                dataGridView1.Columns["Monthly Rental"].FillWeight = 80;
                dataGridView1.Columns["Penalty"].FillWeight = 80;
                dataGridView1.Columns["Payment Status"].FillWeight = 80;

                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    col.ReadOnly = col.Name != "Select";
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                }

                dataGridView1.Columns["Select"].SortMode = DataGridViewColumnSortMode.NotSortable;
                ColorPaymentStatusColumn();
            };

            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
        }

        private void SetupFilters()
        {
            txtSearch.PlaceholderText = "🔍 Search Records: ";
            txtSearch.TextChanged += (s, e) => ApplyFilter();
        }

        // ===================== DATA LOADING ===================== //
        public void LoadProfiles()
        {
            dataGridView1.SuspendLayout();
            dataGridView1.DataSource = null;

            dtProfiles = Database.GetAllProfiles();
            dataGridView1.DataSource = dtProfiles;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.ReadOnly = col.Name != "Select";
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
            }

            FormatCurrencyColumn("Monthly Rental");
            FormatCurrencyColumn("Penalty");
            FormatCurrencyColumn("Additional Charge");
            ColorPaymentStatusColumn();

            lblTotalRecords.Text = $"Total Records: {dtProfiles.Rows.Count}";
            _sortAscending = true;
            _lastSortedCol = -1;

            dataGridView1.ResumeLayout();
            LoadStatistics();
        }

        private void FormatCurrencyColumn(string columnName)
        {
            var column = dataGridView1.Columns[columnName];
            if (column == null) return;

            column.DefaultCellStyle.Format = "C2";
            column.DefaultCellStyle.FormatProvider = new CultureInfo("en-PH");
        }

        // ===================== SORTING ===================== //
        private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Select")
            {
                bool allChecked = dataGridView1.Rows
                    .Cast<DataGridViewRow>()
                    .All(r => Convert.ToBoolean(r.Cells["Select"].Value));

                foreach (DataGridViewRow row in dataGridView1.Rows)
                    row.Cells["Select"].Value = !allChecked;

                return;
            }

            string headerText = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            if (_lastSortedCol == e.ColumnIndex)
                _sortAscending = !_sortAscending;
            else
            {
                _sortAscending = true;
                _lastSortedCol = e.ColumnIndex;
            }

            string sortDir = _sortAscending ? "ASC" : "DESC";

            if (headerText == "SIN")
            {
                static int ParseSinSuffix(DataRow r)
                {
                    var parts = r["SIN"].ToString()!.Split('-');
                    return parts.Length >= 3 && int.TryParse(parts[2], out int n) ? n : 0;
                }

                var rows = dtProfiles.AsEnumerable();
                var sorted = (_sortAscending
                    ? rows.OrderBy(ParseSinSuffix)
                    : rows.OrderByDescending(ParseSinSuffix))
                    .CopyToDataTable();

                dataGridView1.DataSource = sorted;
            }
            else
            {
                dtProfiles.DefaultView.Sort = $"[{headerText}] {sortDir}";
                dataGridView1.DataSource = dtProfiles.DefaultView.ToTable();
            }

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.ReadOnly = col.Name != "Select";
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
            }

            FormatCurrencyColumn("Monthly Rental");
            FormatCurrencyColumn("Penalty");
            FormatCurrencyColumn("Additional Charge");
            ColorPaymentStatusColumn();

            dataGridView1.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
                _sortAscending ? SortOrder.Ascending : SortOrder.Descending;

            lblTotalRecords.Text = $"Total Records: {dataGridView1.Rows.Count}";
        }

        // ===================== FILTERING ===================== //
        private void ApplyFilter()
        {
            try
            {
                string search = txtSearch.Text.Trim().Replace("'", "''");

                if (string.IsNullOrWhiteSpace(search))
                {
                    dtProfiles.DefaultView.RowFilter = string.Empty;
                    lblTotalRecords.Text = $"Total Records: {dtProfiles.Rows.Count}";
                    return;
                }

                dtProfiles.DefaultView.RowFilter = $@"
                    Convert([SIN], 'System.String')            LIKE '%{search}%' OR
                    [Full Name]                                LIKE '%{search}%' OR
                    [Business Name]                            LIKE '%{search}%' OR
                    [Business Section]                         LIKE '%{search}%' OR
                    Convert([Stall Number], 'System.String')   LIKE '%{search}%' OR
                    Convert([Stall Size], 'System.String')     LIKE '%{search}%' OR
                    Convert([Monthly Rental], 'System.String') LIKE '%{search}%' OR
                    [Payment Status]                           = '{search}'      OR
                    Convert([Penalty], 'System.String')        LIKE '%{search}%' OR
                    [Date of Occupancy]                        LIKE '%{search}%'";

                lblTotalRecords.Text = $"Total Records: {dtProfiles.DefaultView.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filter error: {ex.Message}");
            }
        }

        // ===================== EDIT RECORD ===================== //
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Select") return;

            var row = dataGridView1.Rows[e.RowIndex];

            string sin = row.Cells["SIN"].Value?.ToString() ?? "";
            string fullName = row.Cells["Full Name"].Value?.ToString() ?? "";
            string businessName = row.Cells["Business Name"].Value?.ToString() ?? "";
            string businessSection = row.Cells["Business Section"].Value?.ToString() ?? "";
            string stallNumber = row.Cells["Stall Number"].Value?.ToString() ?? "";
            string stallSize = row.Cells["Stall Size"].Value?.ToString() ?? "";
            string monthlyRental = row.Cells["Monthly Rental"].Value?.ToString() ?? "";
            string paymentStatus = row.Cells["Payment Status"].Value?.ToString() ?? "Unpaid";
            string penalty = row.Cells["Penalty"].Value?.ToString() ?? "0.00";
            string startDate = row.Cells["Date of Occupancy"].Value?.ToString() ?? "";
            decimal additionalCharge = Convert.ToDecimal(row.Cells["Additional Charge"].Value ?? 0);

            var form = new ProfilingForm();
            form.LoadForEdit(sin, fullName, businessName, businessSection,
                stallNumber, stallSize, monthlyRental, paymentStatus,
                startDate, penalty, additionalCharge);
            form.ShowDialog();

            if (form.RecordSaved)
            {
                int firstRowIndex = dataGridView1.FirstDisplayedScrollingRowIndex;
                LoadProfiles();

                if (firstRowIndex >= 0 && firstRowIndex < dataGridView1.RowCount)
                    dataGridView1.FirstDisplayedScrollingRowIndex = firstRowIndex;

                HighlightRecord(sin);
            }
        }

        // ===================== GENERATE RECEIPT ===================== //
        private void button2_Click(object sender, EventArgs e)
        {
            var checkedRows = dataGridView1.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells["Select"].Value))
                .ToList();

            if (checkedRows.Count == 0)
            {
                MessageBox.Show("Please check at least one record.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (checkedRows.Count > 3)
            {
                MessageBox.Show("Please select a maximum of 3 records at a time.", "Too Many Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedProfiles = new List<BillingReportModel>();

            foreach (DataGridViewRow row in checkedRows)
            {
                string paymentStatus = row.Cells["Payment Status"].Value?.ToString() ?? "";

                if (paymentStatus == "Paid")
                {
                    MessageBox.Show(
                        $"{row.Cells["Full Name"].Value} has already paid. Skipping.",
                        "Already Paid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                selectedProfiles.Add(new BillingReportModel
                {
                    SIN = row.Cells["SIN"].Value?.ToString()!,
                    FullName = row.Cells["Full Name"].Value?.ToString()!,
                    BusinessName = row.Cells["Business Name"].Value?.ToString()!,
                    BusinessSection = row.Cells["Business Section"].Value?.ToString()!,
                    StallNumber = row.Cells["Stall Number"].Value?.ToString()!,
                    StallSize = row.Cells["Stall Size"].Value?.ToString()!,
                    MonthlyRental = Convert.ToDecimal(row.Cells["Monthly Rental"].Value ?? 0),
                    PaymentStatus = paymentStatus,
                    Penalty = Convert.ToDecimal(row.Cells["Penalty"].Value ?? 0),
                    AdditionalCharge = Convert.ToDecimal(row.Cells["Additional Charge"].Value ?? 0),
                    StartDate = row.Cells["Date of Occupancy"].Value?.ToString() ?? "",
                });
            }

            if (selectedProfiles.Count == 0)
            {
                MessageBox.Show("No valid unpaid records selected.");
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells["Select"].Value = false;

            new ReportViewerForm(selectedProfiles).ShowDialog();
        }

        // ===================== PAYMENT HISTORY ===================== //
        private void btnPaymentHistory_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a record first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dataGridView1.SelectedRows[0];
            string sin = row.Cells["SIN"].Value?.ToString() ?? "";
            string fullName = row.Cells["Full Name"].Value?.ToString() ?? "";
            string businessName = row.Cells["Business Name"].Value?.ToString() ?? "";

            DataTable history = Database.GetPaymentHistory(sin);

            if (history.Rows.Count == 0)
            {
                MessageBox.Show($"No payment history found for {businessName}.",
                    "No Records", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            new PaymentHistoryForm(sin, fullName, businessName, history).ShowDialog();

            LoadProfiles();
            HighlightRecord(sin);
        }

        // ===================== EXPORT TO EXCEL ===================== //
        private async void btnExport_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel Workbook|*.xlsx";
            sfd.FileName = "Stall_Owners_Profiling.xlsx";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                btnExport.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var dt = GetDataTableFromDGV(dataGridView1);
                await Task.Run(() => ExportToExcel(dt, sfd.FileName));

                MessageBox.Show("Export completed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExport.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private DataTable GetDataTableFromDGV(DataGridView dgv)
        {
            var dt = new DataTable();

            foreach (DataGridViewColumn col in dgv.Columns)
                if (col.Name != "Select")
                    dt.Columns.Add(col.HeaderText);

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                dt.Rows.Add(row.Cells
                    .Cast<DataGridViewCell>()
                    .Where(c => c.OwningColumn.Name != "Select")
                    .Select(c => c.Value?.ToString() ?? "")
                    .ToArray());
            }

            return dt;
        }

        private void ExportToExcel(DataTable dt, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Profiles");

            ws.Cell(1, 1).Value = "Stall Owners Profiling Report";
            ws.Range(1, 1, 1, dt.Columns.Count).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;

            ws.Cell(3, 1).InsertTable(dt);
            ws.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        // ===================== MONTHLY REPORT ===================== //
        private void GenerateMonthlyReport(
            int fromMonth, int fromYear,
            int toMonth, int toYear,
            string rangeLabel, string filePath)
        {
            DataTable dt = Database.GetMonthlyReport(fromMonth, fromYear, toMonth, toYear);

            int totalRecords = dt.Rows.Count;
            int totalPaid = dt.Select("[Payment Status] = 'Paid'").Length;
            int totalUnpaid = dt.Select("[Payment Status] = 'Unpaid'").Length;
            decimal totalCollected = dt.Select("[Payment Status] = 'Paid'")
                                         .Sum(r => Convert.ToDecimal(r["Monthly Rental"]));
            decimal totalUncollected = dt.Select("[Payment Status] = 'Unpaid'")
                                         .Sum(r => Convert.ToDecimal(r["Monthly Rental"]));
            decimal totalPenalty = dt.Select("[Payment Status] = 'Unpaid'")
                                         .Sum(r => Convert.ToDecimal(r["Penalty"]));
            decimal grandTotal = totalCollected + totalPenalty;

            var phpCulture = new CultureInfo("en-PH");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Monthly Report");

            ws.Cell("A1").Value = "Municipality of Masinloc";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Range("A1:H1").Merge();
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Value = "Business Permit & Licensing Office";
            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Range("A2:H2").Merge();
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Value = $"Monthly Collection Summary Report — {rangeLabel}";
            ws.Cell("A3").Style.Font.Bold = true;
            ws.Cell("A3").Style.Font.FontSize = 11;
            ws.Range("A3:H3").Merge();
            ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A4").Value = $"Generated by: {Session.CurrentFullName} | {Session.CurrentPosition}";
            ws.Range("A4:H4").Merge();
            ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A5").Value = $"Date Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            ws.Range("A5:H5").Merge();
            ws.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 7;
            var headerFill = XLColor.FromArgb(0, 51, 102);
            var headerFont = XLColor.White;

            ws.Cell(row, 1).Value = "SUMMARY";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Font.FontColor = headerFont;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = headerFill;
            ws.Range(row, 1, row, 8).Merge();
            row++;

            void AddSummaryRow(string label, string value)
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 5).Value = value;
                ws.Range(row, 5, row, 8).Merge();
                row++;
            }

            AddSummaryRow("Total Stall Owners", totalRecords.ToString());
            AddSummaryRow("Total Paid", totalPaid.ToString());
            AddSummaryRow("Total Unpaid", totalUnpaid.ToString());
            AddSummaryRow("Total Collected", totalCollected.ToString("C2", phpCulture));
            AddSummaryRow("Total Uncollected", totalUncollected.ToString("C2", phpCulture));
            AddSummaryRow("Total Penalty Collected", totalPenalty.ToString("C2", phpCulture));
            AddSummaryRow("Grand Total", grandTotal.ToString("C2", phpCulture));

            row++;

            string[] headers = {
                "SIN", "Full Name", "Business Name", "Business Section",
                "Monthly Rental", "Penalty", "Additional Charge", "Payment Status"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = headerFont;
                cell.Style.Fill.BackgroundColor = headerFill;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            row++;

            bool alternate = false;
            foreach (DataRow dr in dt.Rows)
            {
                var rowColor = alternate ? XLColor.FromArgb(240, 244, 255) : XLColor.White;

                ws.Cell(row, 1).Value = dr["SIN"].ToString();
                ws.Cell(row, 2).Value = dr["Full Name"].ToString();
                ws.Cell(row, 3).Value = dr["Business Name"].ToString();
                ws.Cell(row, 4).Value = dr["Business Section"].ToString();

                ws.Cell(row, 5).Value = Convert.ToDecimal(dr["Monthly Rental"]);
                ws.Cell(row, 5).Style.NumberFormat.Format = "₱#,##0.00";

                ws.Cell(row, 6).Value = Convert.ToDecimal(dr["Penalty"]);
                ws.Cell(row, 6).Style.NumberFormat.Format = "₱#,##0.00";

                ws.Cell(row, 7).Value = Convert.ToDecimal(dr["Additional Charge"]);
                ws.Cell(row, 7).Style.NumberFormat.Format = "₱#,##0.00";

                ws.Cell(row, 8).Value = dr["Payment Status"].ToString();

                var statusCell = ws.Cell(row, 8);
                if (dr["Payment Status"].ToString() == "Paid")
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    statusCell.Style.Font.FontColor = XLColor.DarkGreen;
                }
                else
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                    statusCell.Style.Font.FontColor = XLColor.DarkRed;
                }

                for (int col = 1; col <= 7; col++)
                    ws.Cell(row, col).Style.Fill.BackgroundColor = rowColor;

                alternate = !alternate;
                row++;
            }

            ws.Columns().AdjustToContents();
            foreach (var col in ws.ColumnsUsed())
                if (col.Width < 15) col.Width = 15;
            workbook.SaveAs(filePath);
        }

        private void btnMonthlyReport_Click_1(object sender, EventArgs e)
        {
            using var form = new Form();
            form.Text = "Select Date Range";
            form.Size = new Size(320, 200);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.BackColor = Color.White;
            form.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "Masinloc-Logo-HD.ico"));

            string[] months = {
                "January", "February", "March", "April",
                "May", "June", "July", "August",
                "September", "October", "November", "December"
            };

            var lblFrom = new Label { Text = "From:", Location = new Point(15, 20), AutoSize = true };
            var cmbFromMonth = new ComboBox { Location = new Point(80, 17), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            var cmbFromYear = new ComboBox { Location = new Point(200, 17), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            var lblTo = new Label { Text = "To:", Location = new Point(15, 57), AutoSize = true };
            var cmbToMonth = new ComboBox { Location = new Point(80, 54), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            var cmbToYear = new ComboBox { Location = new Point(200, 54), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };

            cmbFromMonth.Items.AddRange(months);
            cmbToMonth.Items.AddRange(months);
            cmbFromMonth.SelectedIndex = 0;
            cmbToMonth.SelectedIndex = DateTime.Now.Month - 1;

            for (int y = DateTime.Now.Year; y >= DateTime.Now.Year - 5; y--)
            {
                cmbFromYear.Items.Add(y);
                cmbToYear.Items.Add(y);
            }
            cmbFromYear.SelectedIndex = 0;
            cmbToYear.SelectedIndex = 0;

            var btnGenerate = new Button
            {
                Text = "Generate",
                Location = new Point(80, 110),
                Width = 140,
                Height = 30,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            btnGenerate.Click += (s, ev) =>
            {
                int fromMonth = cmbFromMonth.SelectedIndex + 1;
                int fromYear = (int)cmbFromYear.SelectedItem!;
                int toMonth = cmbToMonth.SelectedIndex + 1;
                int toYear = (int)cmbToYear.SelectedItem!;

                if (new DateTime(fromYear, fromMonth, 1) > new DateTime(toYear, toMonth, 1))
                {
                    MessageBox.Show("From date cannot be later than To date.",
                        "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                form.DialogResult = DialogResult.OK;
            };

            form.Controls.AddRange(new Control[] {
                lblFrom, cmbFromMonth, cmbFromYear,
                lblTo,   cmbToMonth,   cmbToYear,
                btnGenerate
            });

            if (form.ShowDialog() != DialogResult.OK) return;

            int selectedFromMonth = cmbFromMonth.SelectedIndex + 1;
            int selectedFromYear = (int)cmbFromYear.SelectedItem!;
            int selectedToMonth = cmbToMonth.SelectedIndex + 1;
            int selectedToYear = (int)cmbToYear.SelectedItem!;

            string rangeLabel = selectedFromMonth == selectedToMonth && selectedFromYear == selectedToYear
                ? $"{months[selectedFromMonth - 1]} {selectedFromYear}"
                : $"{months[selectedFromMonth - 1]} {selectedFromYear} - {months[selectedToMonth - 1]} {selectedToYear}";

            using var sfd = new SaveFileDialog();
            sfd.Filter = "Excel Workbook|*.xlsx";
            sfd.FileName = $"MonthlyReport_{rangeLabel.Replace(" ", "_").Replace("-", "to")}.xlsx";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                GenerateMonthlyReport(
                    selectedFromMonth, selectedFromYear,
                    selectedToMonth, selectedToYear,
                    rangeLabel, sfd.FileName);

                MessageBox.Show("Monthly Report exported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // ===================== IMPORT ===================== //
        private async void btnImport_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Title = "Select Import File";
            ofd.Filter = "Supported Files|*.csv;*.xlsx|CSV Files|*.csv|Excel Files|*.xlsx";

            if (ofd.ShowDialog() != DialogResult.OK) return;

            string filePath = ofd.FileName;
            string ext = Path.GetExtension(filePath).ToLower();

            btnImport.Enabled = false;
            Cursor = Cursors.WaitCursor;
            lblTotalRecords.Text = "Importing... please wait.";

            try
            {
                var (imported, skipped) = await Task.Run(() =>
                {
                    var existingSINs = Database.GetAllSINs();

                    List<ImportRow> rows = ext == ".csv"
                        ? ReadCsv(filePath)
                        : ReadExcel(filePath);

                    return ImportToDatabase(rows, existingSINs);
                });

                LoadProfiles();

                string summary = $"Import Complete!\n\n" +
                                 $"✅ Imported : {imported} records\n" +
                                 $"⚠️ Skipped  : {skipped.Count} records";

                if (skipped.Count > 0)
                {
                    summary += "\n\nSkipped Records:\n";
                    foreach (var s in skipped)
                        summary += $"  - {s}\n";
                }

                MessageBox.Show(summary, "Import Result",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnImport.Enabled = true;
                Cursor = Cursors.Default;
                lblTotalRecords.Text = $"Total Records: {dtProfiles.Rows.Count}";
            }
        }

        private List<ImportRow> ReadCsv(string filePath)
        {
            var rows = new List<ImportRow>();
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvHelper.CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                rows.Add(new ImportRow
                {
                    SIN = csv.GetField("SIN") ?? "",
                    FullName = csv.GetField("FullName") ?? "",
                    BusinessName = csv.GetField("BusinessName") ?? "",
                    BusinessSection = csv.GetField("BusinessSection") ?? "",
                    StallNumber = csv.GetField("StallNumber") ?? "",
                    StallSize = csv.GetField("StallSize") ?? "",
                    MonthlyRental = csv.GetField("MonthlyRental") ?? "",
                    PaymentStatus = csv.GetField("PaymentStatus") ?? "",
                    StartDate = csv.GetField("StartDate") ?? "",
                    Penalty = csv.GetField("Penalty") ?? "",
                    AdditionalCharge = csv.GetField("AdditionalCharge") ?? "",
                });
            }

            return rows;
        }

        private List<ImportRow> ReadExcel(string filePath)
        {
            var rows = new List<ImportRow>();

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);

            // Read header row to build a name→index map
            var headerRow = ws.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
                headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;

            string Get(IXLRow row, string colName)
                => headers.TryGetValue(colName, out int col)
                    ? row.Cell(col).GetString()
                    : "";

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                rows.Add(new ImportRow
                {
                    SIN = Get(row, "SIN"),
                    FullName = Get(row, "FullName"),
                    BusinessName = Get(row, "BusinessName"),
                    BusinessSection = Get(row, "BusinessSection"),
                    StallNumber = Get(row, "StallNumber"),
                    StallSize = Get(row, "StallSize"),
                    MonthlyRental = Get(row, "MonthlyRental"),
                    PaymentStatus = Get(row, "PaymentStatus"),
                    StartDate = Get(row, "StartDate"),
                    Penalty = Get(row, "Penalty"),
                    AdditionalCharge = Get(row, "AdditionalCharge"),
                });
            }

            return rows;
        }

        private (int Imported, List<string> Skipped) ImportToDatabase(
            List<ImportRow> rows,
            HashSet<string> existingSINs)
        {
            int imported = 0;
            var skipped = new List<string>();
            var validRows = new DataTable();

            validRows.Columns.Add("SIN");
            validRows.Columns.Add("FullName");
            validRows.Columns.Add("BusinessName");
            validRows.Columns.Add("BusinessSection");
            validRows.Columns.Add("StallNumber");
            validRows.Columns.Add("StallSize");
            validRows.Columns.Add("MonthlyRental", typeof(decimal));
            validRows.Columns.Add("PaymentStatus");
            validRows.Columns.Add("StartDate");
            validRows.Columns.Add("Penalty", typeof(decimal));
            validRows.Columns.Add("AdditionalCharge", typeof(decimal));
            validRows.Columns.Add("IsArchived", typeof(int));

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.SIN) || string.IsNullOrWhiteSpace(row.FullName))
                {
                    skipped.Add("Empty row skipped");
                    continue;
                }

                if (existingSINs.Contains(row.SIN))
                {
                    skipped.Add($"SIN {row.SIN} — duplicate SIN");
                    continue;
                }

                decimal.TryParse(row.MonthlyRental, out decimal monthlyRental);
                decimal.TryParse(row.Penalty, out decimal penalty);
                decimal.TryParse(row.AdditionalCharge, out decimal additionalCharge);

                validRows.Rows.Add(
                    row.SIN, row.FullName, row.BusinessName, row.BusinessSection,
                    row.StallNumber, row.StallSize, monthlyRental, row.PaymentStatus,
                    row.StartDate, penalty, additionalCharge, 0);

                existingSINs.Add(row.SIN);
                imported++;
            }

            if (validRows.Rows.Count > 0)
            {
                var result = Database.ImportProfiling(validRows);
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);
            }

            return (imported, skipped);
        }

        // ===================== STATISTICS ===================== //
        private void LoadStatistics()
        {
            var (_, paid, unpaid) = Database.GetPaymentSummary();
            var (totalCollected, totalUncollected, totalPenalty) = Database.GetCollectionSummary();

            lblTotalPaid.Text = paid.ToString("N0");
            lblTotalUnpaid.Text = unpaid.ToString("N0");
            lblTotalCollected.Text = totalCollected.ToString("C2", new CultureInfo("en-PH"));
            lblTotalUncollected.Text = totalUncollected.ToString("C2", new CultureInfo("en-PH"));
            lblTotalPenalty.Text = totalPenalty.ToString("C2", new CultureInfo("en-PH"));
        }

        // ===================== NAVIGATION ===================== //
        private void button1_Click(object sender, EventArgs e)
        {
            new DashboardForm().Show();
            this.Hide();
        }

        // ===================== COLOR CODING ===================== //
        private void ColorPaymentStatusColumn()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                row.DefaultCellStyle.BackColor = row.Index % 2 == 0
                    ? ModernPalette.Surface
                    : ModernPalette.Field;

                var cell = row.Cells["Payment Status"];
                if (cell?.Value == null) continue;

                switch (cell.Value.ToString())
                {
                    case "Paid":
                        cell.Style.BackColor = Color.LightGreen;
                        cell.Style.ForeColor = Color.DarkGreen;
                        break;
                    case "Unpaid":
                        cell.Style.BackColor = Color.LightCoral;
                        cell.Style.ForeColor = Color.DarkRed;
                        break;
                    case "Unverified":
                        cell.Style.BackColor = Color.LightGoldenrodYellow;
                        cell.Style.ForeColor = Color.DarkGoldenrod;
                        break;
                }
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

        // ===================== ARCHIVE ===================== //
        private void btnArchive_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a record first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dataGridView1.SelectedRows[0];
            string sin = row.Cells["SIN"].Value?.ToString() ?? "";
            string name = row.Cells["Full Name"].Value?.ToString() ?? "";

            var confirm = MessageBox.Show(
                $"Archive record for {name}?\n\nArchived records are hidden from the list but kept in the database.",
                "Confirm Archive", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var result = Database.ArchiveProfiling(sin);

            if (result.Success)
            {
                Database.LogAudit("Archive", sin, Session.CurrentUserId ?? 0,
                    $"Archived profile for {name}.");

                MessageBox.Show("Record archived successfully.", "Archived",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadProfiles();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== HIGHLIGHT HELPERS ===================== //
        public void HighlightRecord(string sin)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["SIN"].Value?.ToString() == sin)
                {
                    dataGridView1.ClearSelection();
                    row.Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        public void HighlightLastAdded()
        {
            if (dataGridView1.Rows.Count == 0) return;
            dataGridView1.ClearSelection();
            dataGridView1.Rows[0].Selected = true;
            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
