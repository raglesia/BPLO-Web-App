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

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class VehiclePermitLists : Form
    {
        // ===================== FIELDS ===================== //
        private DataTable _dt = new DataTable(); // working copy
        private DataTable _dtOriginal = new DataTable(); // master copy — never modified
        private bool _sortAsc = true;
        private int _lastSortCol = -1;

        // ===================== CONSTRUCTOR ===================== //
        public VehiclePermitLists()
        {
            InitializeComponent();
            SetupGrid();
            SetupFilters();
            LoadRecords();
        }

        // ===================== FORM LOAD ===================== //
        private void VehiclePermitLists_Load(object sender, EventArgs e)
        {
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";
            btnGenerateReceipt.Focus();

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
            dataGridView1.MultiSelect = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;

            dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
        }

        // ← KEY FIX: mirrors ProfilingLists DataBindingComplete pattern
        // This fires every time DataSource is set, ensuring columns are
        // always properly configured after any sort or reload
        private void DataGridView1_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dataGridView1.Columns.Contains("Date Added"))
                dataGridView1.Columns["Date Added"].Visible = false;

            if (dataGridView1.Columns.Contains("Permit Year"))
                dataGridView1.Columns["Permit Year"].Visible = false;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.ReadOnly = true;
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
            }

            ColorPermitStatusColumn();
            AlternateRowColors();
        }

        private void SetupFilters()
        {
            txtSearch.PlaceholderText = "🔍 Search records...";
            txtSearch.TextChanged += (s, e) => ApplyFilter();
        }

        // ===================== LOAD ===================== //
        public void LoadRecords()
        {
            dataGridView1.SuspendLayout();
            dataGridView1.DataSource = null;

            _dtOriginal = Database.GetAllVehiclePermits();
            _dt = _dtOriginal.Copy();
            dataGridView1.DataSource = _dt;

            lblTotalRecords.Text = $"Total Records: {_dt.Rows.Count}";
            _sortAsc = true;
            _lastSortCol = -1;

            dataGridView1.ResumeLayout();
            LoadStatistics();
        }

        // ===================== SORTING ===================== //
        // KEY FIX: always sort from _dtOriginal, same as dtProfiles in ProfilingLists
        private void dataGridView1_ColumnHeaderMouseClick(
            object? sender, DataGridViewCellMouseEventArgs e)
        {
            string header = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            if (_lastSortCol == e.ColumnIndex)
                _sortAsc = !_sortAsc;
            else
            {
                _sortAsc = true;
                _lastSortCol = e.ColumnIndex;
            }

            string dir = _sortAsc ? "ASC" : "DESC";

            if (header == "VIN")
            {
                static int ParseSuffix(DataRow r)
                {
                    var parts = r["VIN"].ToString()!.Split('-');
                    return parts.Length >= 3 && int.TryParse(parts[2], out int n) ? n : 0;
                }

                // Always sort from _dtOriginal
                var rows = _dtOriginal.AsEnumerable();
                var sorted = (_sortAsc
                    ? rows.OrderBy(ParseSuffix)
                    : rows.OrderByDescending(ParseSuffix))
                    .CopyToDataTable();

                dataGridView1.DataSource = sorted;
            }
            else
            {
                // Always sort from _dtOriginal, same as dtProfiles.DefaultView in ProfilingLists
                _dtOriginal.DefaultView.Sort = $"[{header}] {dir}";
                dataGridView1.DataSource = _dtOriginal.DefaultView.ToTable();
            }

            dataGridView1.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
                _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

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
                    _dt.DefaultView.RowFilter = string.Empty;
                    lblTotalRecords.Text = $"Total Records: {_dt.Rows.Count}";
                    return;
                }

                _dt.DefaultView.RowFilter = $@"
                    [VIN]          LIKE '%{search}%' OR
                    [Company Name] LIKE '%{search}%' OR
                    [Driver Name]  LIKE '%{search}%' OR
                    [Plate No]     LIKE '%{search}%' OR
                    [SEC Reg No]   LIKE '%{search}%' OR
                    [DTI Number]   LIKE '%{search}%'";

                lblTotalRecords.Text = $"Total Records: {_dt.DefaultView.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filter error: {ex.Message}");
            }
        }

        // ===================== EDIT (double-click) ===================== //
        private void dataGridView1_CellDoubleClick(
            object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dataGridView1.Rows[e.RowIndex];

            string vin = row.Cells["VIN"].Value?.ToString() ?? "";
            string companyName = row.Cells["Company Name"].Value?.ToString() ?? "";
            string driverName = row.Cells["Driver Name"].Value?.ToString() ?? "";
            string plateNo = row.Cells["Plate No"].Value?.ToString() ?? "";
            string secRegNo = row.Cells["SEC Reg No"].Value?.ToString() ?? "";
            string dtiNumber = row.Cells["DTI Number"].Value?.ToString() ?? "";

            var form = new VehicleProfiling();
            int.TryParse(row.Cells["Permit Year"].Value?.ToString(), out int permitYear);
            string permitStatus = row.Cells["Permit Status"].Value?.ToString() ?? "Unpaid";
            form.LoadForEdit(vin, companyName, driverName, plateNo, secRegNo, dtiNumber,
                permitYear, permitStatus);
            form.ShowDialog();

            if (form.RecordSaved)
            {
                int firstRow = dataGridView1.FirstDisplayedScrollingRowIndex;
                LoadRecords();
                if (firstRow >= 0 && firstRow < dataGridView1.RowCount)
                    dataGridView1.FirstDisplayedScrollingRowIndex = firstRow;
                HighlightRecord(vin);
            }
        }

        // ===================== ADD ===================== //
        private void btnAdd_Click(object sender, EventArgs e)
        {
            var form = new VehicleProfiling();
            form.ShowDialog();

            if (form.RecordSaved)
            {
                LoadRecords();
                HighLightLastAdded();
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
            string vin = row.Cells["VIN"].Value?.ToString() ?? "";
            string name = row.Cells["Company Name"].Value?.ToString() ?? "";

            var confirm = MessageBox.Show(
                $"Archive \"{name}\"?\n\nRecord will be hidden but kept in the database.",
                "Confirm Archive", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var result = Database.ArchiveVehiclePermit(vin);

            if (result.Success)
            {
                Database.LogAudit("Archive Vehicle Permit", vin,
                    Session.CurrentUserId ?? 0, $"Archived: {name}");
                MessageBox.Show("Record archived.", "Archived",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadRecords();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== EXPORT ===================== //
        private async void btnExport_Click(object sender, EventArgs e)
        {
            if (_dt.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "Excel Workbook|*.xlsx";
            sfd.FileName = "VehiclePermits.xlsx";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                btnExport.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var dt = _dt.Copy();
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Vehicle Permits");

                    ws.Cell(1, 1).Value = "Special Vehicle Permit Records";
                    ws.Range(1, 1, 1, dt.Columns.Count).Merge();
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 14;
                    ws.Cell(1, 1).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    ws.Cell(3, 1).InsertTable(dt);
                    ws.Columns().AdjustToContents();
                    wb.SaveAs(sfd.FileName);
                });

                MessageBox.Show("Export completed!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExport.Enabled = true;
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
                    var existingPlates = Database.GetAllPlateNumbers();

                    List<VehicleImportRow> rows = ext == ".csv"
                        ? ReadVehicleCsv(filePath)
                        : ReadVehicleExcel(filePath);

                    return ImportVehiclesToDatabase(rows, existingPlates);
                });

                LoadRecords();

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
                lblTotalRecords.Text = $"Total Records: {_dt.Rows.Count}";
            }
        }

        private List<VehicleImportRow> ReadVehicleCsv(string filePath)
        {
            var rows = new List<VehicleImportRow>();
            var config = new CsvHelper.Configuration.CsvConfiguration(
                CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvHelper.CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                rows.Add(new VehicleImportRow
                {
                    CompanyName = csv.GetField("CompanyName") ?? "",
                    DriverName = csv.GetField("DriverName") ?? "",
                    PlateNo = csv.GetField("PlateNo") ?? "",
                    SECRegNo = csv.GetField("SECRegNo") ?? "",
                    DTINumber = csv.GetField("DTINumber") ?? "",
                });
            }

            return rows;
        }

        private List<VehicleImportRow> ReadVehicleExcel(string filePath)
        {
            var rows = new List<VehicleImportRow>();

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);
            var dataRows = ws.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                rows.Add(new VehicleImportRow
                {
                    CompanyName = row.Cell(1).GetString(),
                    DriverName = row.Cell(2).GetString(),
                    PlateNo = row.Cell(3).GetString(),
                    SECRegNo = row.Cell(4).GetString(),
                    DTINumber = row.Cell(5).GetString(),
                });
            }

            return rows;
        }

        private (int Imported, List<string> Skipped) ImportVehiclesToDatabase(
            List<VehicleImportRow> rows,
            HashSet<string> existingPlates)
        {
            int imported = 0;
            var skipped = new List<string>();
            var validRows = new DataTable();

            validRows.Columns.Add("VIN");
            validRows.Columns.Add("CompanyName");
            validRows.Columns.Add("DriverName");
            validRows.Columns.Add("PlateNo");
            validRows.Columns.Add("SECRegNo");
            validRows.Columns.Add("DTINumber");

            string year = DateTime.Now.Year.ToString();
            int nextNumber = 1;

            using (var con = new Microsoft.Data.SqlClient.SqlConnection(
                Database.GetConnectionString()))
            {
                con.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT ISNULL(MAX(CAST(RIGHT(VIN, 4) AS INT)), 0)
                    FROM   VehiclePermits
                    WHERE  VIN LIKE @pattern", con);
                cmd.Parameters.AddWithValue("@pattern", $"VIN-{year}-%");
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                    nextNumber = Convert.ToInt32(result) + 1;
            }

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.CompanyName) &&
                    string.IsNullOrWhiteSpace(row.PlateNo))
                {
                    skipped.Add("Empty row skipped");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.PlateNo))
                {
                    skipped.Add($"{row.CompanyName} — missing plate number");
                    continue;
                }

                string plateUpper = row.PlateNo.Trim().ToUpper();

                if (existingPlates.Contains(plateUpper))
                {
                    skipped.Add($"Plate {plateUpper} — duplicate, skipped");
                    continue;
                }

                string vin = $"VIN-{year}-{nextNumber:D4}";
                nextNumber++;

                validRows.Rows.Add(
                    vin,
                    row.CompanyName.Trim(),
                    row.DriverName.Trim(),
                    plateUpper,
                    row.SECRegNo.Trim(),
                    row.DTINumber.Trim()
                );

                existingPlates.Add(plateUpper);
                imported++;
            }

            if (validRows.Rows.Count > 0)
            {
                var result = Database.ImportVehiclePermits(validRows);
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);
            }

            return (imported, skipped);
        }

        // ===================== HELPERS ===================== //
        private void AlternateRowColors()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                row.DefaultCellStyle.BackColor = row.Index % 2 == 0
                    ? ModernPalette.Surface
                    : ModernPalette.Field;
            }
        }

        public void HighlightRecord(string vin)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["VIN"].Value?.ToString() == vin)
                {
                    dataGridView1.ClearSelection();
                    row.Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        public void HighLightLastAdded()
        {
            if (dataGridView1.Rows.Count == 0) return;
            dataGridView1.ClearSelection();
            dataGridView1.Rows[0].Selected = true;
            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
        }

        // ===================== NAVIGATION ===================== //
        private void btnMenu_Click(object sender, EventArgs e)
        {
            new DashboardForm().Show();
            this.Hide();
        }

        private void ColorPermitStatusColumn()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                var cell = row.Cells["Permit Status"];
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
                }
            }
        }

        private void LoadStatistics()
        {
            var (total, paid, unpaid) = Database.GetVehiclePermitSummary();
            lblTotalVehicles.Text = $"{total:N0}";
            lblTotalPaid.Text = $"{paid:N0}";
            lblTotalUnpaid.Text = $"{unpaid:N0}";
        }

        private void lblTotalPaid_Click(object sender, EventArgs e)
        {

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
