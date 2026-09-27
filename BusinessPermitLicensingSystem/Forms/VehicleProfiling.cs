using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class VehicleProfiling : Form
    {
        // FIELDS //
        private bool _isEditMode = false;
        private string _editVIN = "";
        public bool RecordSaved { get; private set; } = false;

        // CONSTRUCTOR //
        public VehicleProfiling()
        {
            InitializeComponent();
            InitializeAnnualHistory();
        }

        public void LoadForEdit(
            string vin, string companyName, string driverName,
            string plateNo, string secRegNo, string dtiNumber,
            int permitYear = 0, string permitStatus = "Unpaid")
        {
            _isEditMode = true;
            _editVIN = vin;
            _permitYear = permitYear;
            _permitStatus = permitStatus;

            txtVIN.Text = vin;
            txtCompanyName.Text = companyName;
            txtDriverName.Text = driverName;
            txtPlateNumber.Text = plateNo;
            txtSec.Text = secRegNo;
            txtDTI.Text = dtiNumber;

            txtVIN.ReadOnly = true;
            Text = $"Vehicle Profiling | {plateNo}";
        }

        private void VehicleProfiling_Load(object sender, EventArgs e)

        {
            lblUsername.Text = $"{Session.CurrentFullName} | {Session.CurrentPosition}";

            if (!_isEditMode)
            {
                txtVIN.Text = Database.GenerateUniqueVIN();
                txtVIN.ReadOnly = true;
            }

            if (_isEditMode) LoadAnnualHistory();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string vin = txtVIN.Text.Trim();
            string companyName = txtCompanyName.Text.Trim();
            string driverName = txtDriverName.Text.Trim();
            string plateNo = txtPlateNumber.Text.Trim().ToUpper();
            string secRegNo = txtSec.Text.Trim();
            string dtiNumber = txtDTI.Text.Trim();

            if (string.IsNullOrWhiteSpace(companyName))
            {
                MessageBox.Show("Company name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCompanyName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(plateNo))
            {
                MessageBox.Show("Plate number is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlateNumber.Focus();
                return;
            }

            (bool success, string? error) = _isEditMode
                ? Database.UpdateVehiclePermit(vin, companyName, driverName,
                    plateNo, secRegNo, dtiNumber)
                : Database.AddVehiclePermit(vin, companyName, driverName,
                    plateNo, secRegNo, dtiNumber);

            if (!success)
            {
                MessageBox.Show(error ?? "Failed to save.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Database.LogAudit(
                _isEditMode ? "Update Vehicle Permit" : "Add Vehicle Permit",
                vin, Session.CurrentUserId ?? 0,
                $"{companyName} | Plate: {plateNo}");

            RecordSaved = true;
            MessageBox.Show("Record saved successfully.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        // ===================== EVENTS ===================== //
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
