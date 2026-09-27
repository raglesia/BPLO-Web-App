using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class AuditTrail : Form
    {
        // ===================== CONSTRUCTOR ===================== //
        public AuditTrail()
        {
            InitializeComponent();
            SetupGrid();
            radioUsers.Checked = true;
            LoadAuditRecords();
        }

        // ===================== FORM LOAD ===================== //
        private void AuditTrail_Load(object sender, EventArgs e)
        {
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";
            radioProfiling.Focus();

            this.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));
        }

        // ===================== SETUP ===================== //
        private void SetupGrid()
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(dtAudit, true);

            dtAudit.ReadOnly = true;
            dtAudit.AllowUserToAddRows = false;
            dtAudit.AllowUserToDeleteRows = false;
            dtAudit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dtAudit.MultiSelect = false;
            dtAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dtAudit.RowHeadersVisible = false;
            dtAudit.BackgroundColor = Color.White;
            dtAudit.BorderStyle = BorderStyle.None;
            dtAudit.Font = new Font("Segoe UI", 9);

            dtAudit.RowsDefaultCellStyle.BackColor = Color.White;
            dtAudit.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dtAudit.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        // ===================== DATA LOADING ===================== //
        private void LoadAuditRecords()
        {
            try
            {
                DataTable dt = radioUsers.Checked
                    ? Database.GetUserAuditTrail()
                    : Database.GetAuditTrail();

                dtAudit.DataSource = dt;

                // ✅ Hide internal ID columns
                if (dtAudit.Columns["Id"] != null) dtAudit.Columns["Id"].Visible = false;
                if (dtAudit.Columns["UserId"] != null) dtAudit.Columns["UserId"].Visible = false;

                // ✅ Disable sorting on all visible columns
                foreach (DataGridViewColumn col in dtAudit.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    col.FillWeight = 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load audit records: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== RADIO BUTTONS ===================== //
        private void radioUsers_CheckedChanged(object sender, EventArgs e)
        {
            if (radioUsers.Checked) LoadAuditRecords();
        }

        private void radioProfiling_CheckedChanged(object sender, EventArgs e)
        {
            if (radioProfiling.Checked) LoadAuditRecords();
        }

        // ===================== NAVIGATION ===================== //
        private void button1_Click(object sender, EventArgs e)
        {
            new DashboardForm().Show();
            this.Hide();
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