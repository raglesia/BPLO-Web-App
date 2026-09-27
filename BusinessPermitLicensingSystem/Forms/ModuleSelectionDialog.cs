using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class ModuleSelectionDialog : Form
    {
        public enum Selection { None, StallOwner, VehiclePermit }
        public Selection SelectedModule { get; private set; } = Selection.None;

        public ModuleSelectionDialog()
        {
            InitializeComponent();

            this.Text = "Select Module";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(560, 240);
            this.BackColor = Color.FromArgb(235, 240, 250);

            var lblTitle = new Label
            {
                Text = "Select Profiling Module",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 100),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 15, 560, 50)
            };

            var btnStall = new Button
            {
                Text = "Stall Owners",
                Font = new Font("Segoe UI", 11),
                Bounds = new Rectangle(30, 80, 230, 80),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStall.FlatAppearance.BorderSize = 0;

            var btnVehicle = new Button
            {
                Text = "Special Vehicle Permit",
                Font = new Font("Segoe UI", 10),
                Bounds = new Rectangle(300, 80, 230, 80),
                BackColor = Color.FromArgb(60, 140, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnVehicle.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9),
                Bounds = new Rectangle(230, 185, 100, 32),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnStall.Click += (_, __) => { SelectedModule = Selection.StallOwner; this.Close(); };
            btnVehicle.Click += (_, __) => { SelectedModule = Selection.VehiclePermit; this.Close(); };
            btnCancel.Click += (_, __) => { SelectedModule = Selection.None; this.Close(); };

            this.Controls.AddRange(new Control[] { lblTitle, btnStall, btnVehicle, btnCancel });
        }
    }
}