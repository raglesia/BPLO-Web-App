using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class ArchivedForm : Form
    {
        // ===================== CONSTRUCTOR ===================== //
        public ArchivedForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadArchived();
        }

        // ===================== FORM LOAD ===================== //
        private void ArchivedForm_Load(object sender, EventArgs e)
        {
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";
            btnRestore.Focus();

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

            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;

            dataGridView1.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;

                ColorPaymentStatusColumn();
            };
        }

        // ===================== LOAD ===================== //
        private void LoadArchived()
        {
            dataGridView1.DataSource = Database.GetArchivedProfiles();
            ColorPaymentStatusColumn();
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
                MessageBox.Show(
                    $"No payment history found for {businessName}.",
                    "No Records",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            new PaymentHistoryForm(sin, fullName, businessName, history).ShowDialog();
        }

        // ===================== RESTORE ===================== //
        private void btnRestore_Click_1(object sender, EventArgs e)
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
                $"Restore record for {name}?",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var result = Database.RestoreProfiling(sin);

            if (result.Success)
            {
                Database.LogAudit("Restore", sin, Session.CurrentUserId ?? 0,
                    $"Restored profile for {name}.");

                MessageBox.Show("Record restored successfully.", "Restored",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadArchived();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== COLOR CODING ===================== //
        private void ColorPaymentStatusColumn()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                row.DefaultCellStyle.BackColor = row.Index % 2 == 0
                    ? Color.White
                    : Color.AliceBlue;

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
                }
            }
        }

        // ===================== NAVIGATION ===================== //
        private void btnClose_Click(object sender, EventArgs e)
        {
            new DashboardForm().Show();
            this.Close();
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