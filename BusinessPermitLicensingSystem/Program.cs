using BusinessPermitLicensingSystem.Forms;
using Microsoft.Data.SqlClient;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            ModernTheme.Install();

            // ✅ Show setup screen if connection is not configured or unreachable
            if (!IsConnectionValid())
            {
                var setup = new ConnectionSetupForm();

                // ✅ User closed setup without saving — exit
                if (setup.ShowDialog() != DialogResult.OK)
                    return;
            }

            // ✅ Initialize database
            try
            {
                Database.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize the database:\n\n{ex.Message}\n\n" +
                    "Please check your connection settings and try again.",
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new LogInForm());
        }

        // ===================== CONNECTION CHECK ===================== //
        public static bool IsConnectionValid()
        {
            try
            {
                string? cs = ConfigurationManager.ConnectionStrings["BPLS"]?.ConnectionString;

                if (string.IsNullOrWhiteSpace(cs)) return false;

                using var con = new SqlConnection(cs);
                con.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}