using BusinessPermitLicensingSystem.Models;
using Microsoft.Reporting.WinForms;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem
{
    public partial class ReportViewerForm : Form
    {
        // ===================== FIELDS ===================== //
        private readonly ReportViewer _reportViewer;
        private readonly List<BillingReportModel> _profiles;

        // ===================== CONSTRUCTORS ===================== //

        // Single receipt — keeps all existing call sites working unchanged
        public ReportViewerForm(BillingReportModel profile)
            : this(new List<BillingReportModel> { profile }) { }

        // Multiple receipts — called when user selects 2-3 records
        public ReportViewerForm(List<BillingReportModel> profiles)
        {
            InitializeComponent();

            // Assign RowNumber so the RDLC List control can reference each receipt
            for (int i = 0; i < profiles.Count; i++)
                profiles[i].RowNumber = i + 1;

            _profiles = profiles;

            _reportViewer = new ReportViewer { Dock = DockStyle.Fill };
            Controls.Add(_reportViewer);
        }

        // ===================== FORM LOAD ===================== //
        private void ReportViewerForm_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string reportPath = FindReportPath();

            if (reportPath == null)
            {
                MessageBox.Show(
                    "BillingReport.rdlc not found.\n\n" +
                    "Make sure it's in the 'Report' or 'Reports' folder and set to 'Copy if newer'.",
                    "Report File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            LoadReport(reportPath);
        }

        // ===================== REPORT HELPERS ===================== //
        private static string FindReportPath()
        {
            string[] folders = { "Report", "Reports" };

            foreach (string folder in folders)
            {
                string path = Path.Combine(
                    Application.StartupPath, folder, "BillingReport.rdlc");

                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private void LoadReport(string reportPath)
        {
            try
            {
                _reportViewer.LocalReport.ReportPath = reportPath;
                _reportViewer.LocalReport.DataSources.Clear();
                _reportViewer.LocalReport.DataSources.Add(
                    new ReportDataSource("BillingReportDataSet", _profiles));

                _reportViewer.LocalReport.SetParameters(new[]
                {
            new ReportParameter("ProcessedBy", Session.CurrentFullName ?? ""),
            new ReportParameter("Position",    Session.CurrentPosition ?? ""),
        });

                _reportViewer.RefreshReport();
                _reportViewer.SetDisplayMode(DisplayMode.PrintLayout);
                _reportViewer.ZoomMode = ZoomMode.Percent;
                _reportViewer.ZoomPercent = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Message: {ex.Message}\n\n" +
                    $"Inner 1: {ex.InnerException?.Message}\n\n" +
                    $"Inner 2: {ex.InnerException?.InnerException?.Message}\n\n" +
                    $"Inner 3: {ex.InnerException?.InnerException?.InnerException?.Message}",
                    "Report Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

    }
}