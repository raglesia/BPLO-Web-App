using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class DashboardForm : Form
    {
        // ===================== CONSTRUCTOR ===================== //
        public DashboardForm()
        {
            InitializeComponent();
        }

        // ===================== FORM LOAD ===================== //
        private void DashboardForm_Load(object sender, EventArgs e)
        {
            button1.Focus();
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";

            this.Icon = new Icon(Path.Combine(
                Application.StartupPath, "Resources", "MasinlocLogoIcon.ico"));

            UpdateDateTime();
            timer1.Start();
            CheckPenalties();

            Database.EnsureMonthlyBillingForAll();
            Database.ApplyPenaltiesToAll();
        }

        // ===================== NAVIGATION ===================== //
        private void button1_Click(object sender, EventArgs e)
        {
            new ProfilingLists().Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            new ArchivedForm().Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {

            new RentalRatesForm().ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using var dialog = new ModuleSelectionDialog();
            dialog.ShowDialog(this);

            if (dialog.SelectedModule == ModuleSelectionDialog.Selection.StallOwner)
            {
                var profilingForm = new ProfilingForm();

                profilingForm.FormClosed += (s, args) =>
                {
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is ProfilingLists lists)
                        {
                            lists.LoadProfiles();
                            lists.HighlightLastAdded();
                            break;
                        }
                    }
                };

                profilingForm.Show();
            }
            else if (dialog.SelectedModule == ModuleSelectionDialog.Selection.VehiclePermit)
            {
                var vehicleForm = new VehicleProfiling();
                vehicleForm.Show();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //TODO: Implement admin acess control for audit trail

            new AuditTrail().Show();
            this.Hide();
        }

        // ===================== LOGOUT ===================== //
        private void button6_Click(object sender, EventArgs e)
        {
            if (Session.CurrentUserId != null)
            {
                Database.LogAudit(
                    "Logout", null,
                    Session.CurrentUserId ?? 0,
                    $"User '{Session.CurrentUsername}' logged out.");
            }

            Application.Exit();
        }

        // ===================== PENALTIES ===================== //
        private void CheckPenalties()
        {
            try
            {
                int reset = Database.EnsureMonthlyBillingForAll();
                int vehicleReset = Database.ResetAnnualVehiclePermitStatus(); // ← add this
                var (updated, _) = Database.ApplyPenaltiesToAll();

                if (updated > 0)
                {
                    lblPenaltyNotice.ForeColor = Color.DarkRed;
                    lblPenaltyNotice.Text = $"⚠️ {updated} unpaid stall owners have been charged a 25% penalty.";
                }
                else
                {
                    lblPenaltyNotice.ForeColor = Color.SeaGreen;
                    lblPenaltyNotice.Text = reset > 0
                        ? $"✅ New billing cycle started. {reset} record(s) reset to Unpaid."
                        : vehicleReset > 0
                            ? $"✅ New permit year started. {vehicleReset} vehicle permit(s) reset to Unpaid."
                            : "✅ No penalty charges at this time.";
                }

                lblPenaltyNotice.Visible = true;
            }
            catch (Exception ex)
            {
                lblPenaltyNotice.ForeColor = Color.DarkRed;
                lblPenaltyNotice.Text = $"⚠️ Penalty check failed: {ex.Message}";
                lblPenaltyNotice.Visible = true;
            }
        }

        // ===================== DATE & TIME ===================== //
        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            lblDateTime.Text = $"{now:dddd MMMM dd, yyyy}  {now:hh:mm:ss} {now.ToString("tt").ToUpper()}";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            new VehiclePermitLists().Show();
            this.Hide();
        }

        private void lblPenaltyNotice_Click(object sender, EventArgs e)
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