using BusinessPermitLicensingSystem.Helpers;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace BusinessPermitLicensingSystem.Forms
{
    public partial class ProfilingForm : Form
    {
        // ===================== FIELDS ===================== //
        private bool isEditMode = false;
        private string currentSIN = "";
        private bool isLoading = true;

        private string _editFullName = "";
        private string _editBusinessName = "";
        private string _editBusinessSection = "";
        private string _editStallNumber = "";
        private string _editStallSize = "";
        private string _editMonthlyRental = "";
        private string _editPaymentStatus = "";
        private string _editStartDate = "";
        private string _editPenalty = "";
        private decimal _editAdditionalCharge = 0;


        // ===================== CONSTRUCTOR ===================== //
        public ProfilingForm()
        {
            InitializeComponent();
        }


        // ===================== FORM LOAD ===================== //
        private void ProfilingForm_Load(object sender, EventArgs e)
        {
            this.Icon = new Icon(Path.Combine(
                Application.StartupPath,
                "Resources",
                "MasinlocLogoIcon.ico"));

            txtFName.Focus();

            // Prevent events from recalculating while
            // controls are being populated.
            isLoading = true;

            lblUsername.Text =
                $"{Session.CurrentFullName} | {Session.CurrentPosition}";

            SetupInputValidators();
            SetupControls();
            SetupSecurity();

            if (isEditMode)
            {
                PopulateEditFields();
            }
            else
            {
                AssignNewSIN();
            }

            // Controls are now ready.
            isLoading = false;

            // When editing, use the penalty that came
            // from the ProfilingLists DataGridView.
            UpdateTotalAmountDue(useLoadedPenalty: isEditMode);
        }


        // ===================== SETUP ===================== //
        private void SetupInputValidators()
        {
            txtMRental.TextChanged +=
                (s, e) => UpdateTotalAmountDue();

            txtAdditionalCharge.TextChanged +=
                (s, e) => UpdateTotalAmountDue();

            cmbPaymentStatus.SelectedIndexChanged +=
                (s, e) => UpdateTotalAmountDue();

            chkAdditional.CheckedChanged +=
                (s, e) => UpdateTotalAmountDue();


            txtFName.KeyPress +=
                (s, e) =>
                    InputValidator.AllowOnlyLetters(
                        e,
                        allowDot: true);


            txtSSize.KeyPress +=
                (s, e) =>
                    InputValidator.AllowDecimalNumbers(
                        e,
                        txtSSize);


            txtSNumber.KeyPress +=
                (s, e) =>
                    InputValidator.AllowDigitsAndComma(e);


            txtAdditionalCharge.KeyPress +=
                (s, e) =>
                    InputValidator.AllowDecimalNumbers(
                        e,
                        txtAdditionalCharge);


            cmbBSection.SelectedIndexChanged +=
                (s, e) => ComputeMonthlyRental();

            txtSSize.TextChanged +=
                (s, e) => ComputeMonthlyRental();

            txtAdditionalCharge.TextChanged +=
                (s, e) => ComputeMonthlyRental();


            chkAdditional.CheckedChanged += (s, e) =>
            {
                txtAdditionalCharge.Enabled =
                    chkAdditional.Checked;

                if (!chkAdditional.Checked)
                {
                    txtAdditionalCharge.Text = "0.00";
                }

                ComputeMonthlyRental();
            };
        }


        private void SetupControls()
        {
            txtBIN.Enabled = false;
            txtMRental.Enabled = false;

            // Load business sections.
            cmbBSection.Items.Clear();

            DataTable rates = Database.GetRentalRates();

            foreach (DataRow row in rates.Rows)
            {
                cmbBSection.Items.Add(
                    row["Section"].ToString());
            }


            // Payment status.
            cmbPaymentStatus.Items.Clear();
            cmbPaymentStatus.Items.AddRange(
                new string[]
                {
                    "Unverified",
                    "Unpaid",
                    "Paid"
                });

            cmbPaymentStatus.SelectedIndex = 0;


            // Date.
            dtpStartDate.ShowCheckBox = true;
            dtpStartDate.Checked = false;
            dtpStartDate.Format =
                DateTimePickerFormat.Short;


            // Additional charge.
            txtAdditionalCharge.Enabled = false;
            txtAdditionalCharge.Text = "0.00";


            txtAdditionalCharge.Leave += (s, e) =>
            {
                if (decimal.TryParse(
                    txtAdditionalCharge.Text,
                    out decimal value))
                {
                    txtAdditionalCharge.Text =
                        value.ToString("N2");
                }
            };
        }


        // ===================== LOAD FOR EDIT ===================== //
        public void LoadForEdit(
            string sin,
            string fullName,
            string businessName,
            string businessSection,
            string stallNumber,
            string stallSize,
            string monthlyRental,
            string paymentStatus,
            string startDate,
            string penalty,
            decimal additionalCharge = 0)
        {
            isEditMode = true;
            currentSIN = sin;

            _editFullName = fullName;
            _editBusinessName = businessName;
            _editBusinessSection = businessSection;
            _editStallNumber = stallNumber;
            _editStallSize = stallSize;
            _editMonthlyRental = monthlyRental;
            _editPaymentStatus = paymentStatus;
            _editStartDate = startDate;
            _editPenalty = penalty;
            _editAdditionalCharge = additionalCharge;
        }


        // ===================== POPULATE EDIT FIELDS ===================== //
        private void PopulateEditFields()
        {
            txtBIN.Text = currentSIN;
            txtBIN.Enabled = false;

            txtFName.Text = _editFullName;
            txtBName.Text = _editBusinessName;


            // Business section.
            cmbBSection.SelectedIndex = -1;

            foreach (var item in cmbBSection.Items)
            {
                if (item?.ToString() ==
                    _editBusinessSection)
                {
                    cmbBSection.SelectedItem = item;
                    break;
                }
            }


            // Stall information.
            txtSNumber.Text = _editStallNumber;
            txtSSize.Text = _editStallSize;

            if (decimal.TryParse(
                _editMonthlyRental.Replace(",", ""),
                out decimal loadedRental))
            {
                txtMRental.Text = loadedRental.ToString("N2");
            }
            else
            {
                txtMRental.Text = "0.00";
            }

            // Initially display the penalty that came
            // from ProfilingLists.
            if (decimal.TryParse(
                _editPenalty.Replace(",", ""),
                out decimal loadedPenalty))
            {
                txtPenalty.Text =
                    loadedPenalty.ToString("N2");
            }
            else
            {
                txtPenalty.Text = "0.00";
            }


            // Payment status.
            cmbPaymentStatus.SelectedIndex = -1;

            foreach (var item in cmbPaymentStatus.Items)
            {
                if (item?.ToString() ==
                    _editPaymentStatus)
                {
                    cmbPaymentStatus.SelectedItem = item;
                    break;
                }
            }


            btnSave.Text = "           Update Record";


            // Additional charge.
            if (_editAdditionalCharge > 0)
            {
                chkAdditional.Checked = true;

                txtAdditionalCharge.Enabled = true;

                txtAdditionalCharge.Text =
                    _editAdditionalCharge.ToString("N2");
            }
            else
            {
                chkAdditional.Checked = false;

                txtAdditionalCharge.Enabled = false;

                txtAdditionalCharge.Text = "0.00";
            }


            // Date of occupancy.
            if (!string.IsNullOrWhiteSpace(
                    _editStartDate)
                &&
                DateTime.TryParse(
                    _editStartDate,
                    out DateTime parsedDate))
            {
                dtpStartDate.Value = parsedDate;
                dtpStartDate.Checked = true;
            }
            else
            {
                dtpStartDate.Checked = false;
            }

            // Do NOT call UpdateTotalAmountDue() here.
            // It is called after isLoading becomes false.
        }


        // ===================== COMPUTE ===================== //
        private void ComputeMonthlyRental()
        {
            if (isLoading)
                return;


            string section =
                cmbBSection.SelectedItem?.ToString()
                ?? "";


            if (string.IsNullOrWhiteSpace(section))
                return;


            var (
                ratePerSqm,
                flatRate,
                rateType
            ) = Database.GetRateBySection(section);


            decimal baseRental = 0;


            if (rateType == "Flat")
            {
                baseRental = flatRate;

                txtSSize.Enabled = false;
                txtSSize.Text = "0";
            }
            else
            {
                txtSSize.Enabled = true;

                if (!txtSSize.Enabled ||
                    txtSSize.Text == "0")
                {
                    txtSSize.Text = "";
                    txtMRental.Text = "0.00";
                }


                if (!decimal.TryParse(
                    txtSSize.Text,
                    out decimal stallSize))
                {
                    return;
                }


                if (stallSize <= 0)
                    return;


                baseRental =
                    stallSize * ratePerSqm;
            }


            decimal.TryParse(
                txtAdditionalCharge.Text
                    .Replace(",", ""),
                out decimal additional);


            decimal total =
                baseRental +
                (chkAdditional.Checked
                    ? additional
                    : 0);


            txtMRental.Text =
                total.ToString("N2");
        }


        // ===================== SAVE ===================== //
        private void btnSave_Click(
            object sender,
            EventArgs e)
        {
            string fullName =
                txtFName.Text.Trim();

            string businessName =
                txtBName.Text.Trim();

            string businessSection =
                cmbBSection.SelectedItem
                    ?.ToString() ?? "";

            string stallNumber =
                txtSNumber.Text.Trim();

            string stallSize =
                txtSSize.Text.Trim();

            string paymentStatus =
                cmbPaymentStatus.SelectedItem
                    ?.ToString() ?? "Unpaid";

            string startDate = dtpStartDate.Checked
                ? dtpStartDate.Value.ToString("yyyy-MM-dd")
                : "";

            if (paymentStatus != "Unverified" && startDate == "")
            {
                MessageBox.Show(
                    "Verify the Date of Occupancy before marking this owner Paid or Unpaid.",
                    "Date of Occupancy Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                dtpStartDate.Focus();
                return;
            }


            // ================= VALIDATION ================= //

            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show(
                    "Full Name is required.",
                    "Required Field",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtFName.Focus();
                return;
            }


            if (string.IsNullOrWhiteSpace(
                businessName))
            {
                MessageBox.Show(
                    "Business Name is required.",
                    "Required Field",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtBName.Focus();
                return;
            }


            if (cmbBSection.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Business Section is required.",
                    "Required Field",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbBSection.Focus();
                return;
            }


            if (string.IsNullOrWhiteSpace(
                stallNumber))
            {
                MessageBox.Show(
                    "Stall Number is required.",
                    "Required Field",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtSNumber.Focus();
                return;
            }


            if (string.IsNullOrWhiteSpace(
                    stallSize)
                ||
                stallSize == "0")
            {
                var (_, _, rateType) =
                    Database.GetRateBySection(
                        businessSection);

                if (rateType != "Flat")
                {
                    MessageBox.Show(
                        "Stall Size is required.",
                        "Required Field",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txtSSize.Focus();
                    return;
                }
            }


            if (!decimal.TryParse(
                txtMRental.Text
                    .Replace(",", ""),
                out decimal monthlyRental))
            {
                MessageBox.Show(
                    "Monthly Rental must be a valid number.");

                return;
            }


            decimal.TryParse(
                txtAdditionalCharge.Text
                    .Replace(",", ""),
                out decimal additionalCharge);


            long currentUserId =
                Session.CurrentUserId ?? 0;


            string orNumber = "";
            decimal penalty = 0;


            // If editing and marking as Paid,
            // calculate the latest penalty and ask
            // for OR number.
            if (isEditMode &&
                paymentStatus == "Paid")
            {
                penalty =
                    Database.CalculateOutstandingPenalty(currentSIN);

                var dialog =
                    new ORNumberDialog();


                if (dialog.ShowDialog()
                    != DialogResult.OK)
                {
                    MessageBox.Show(
                        "OR Number is required when marking as Paid.",
                        "OR Number Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }


                orNumber =
                    dialog.ORNumber;
            }


            if (isEditMode)
            {
                SaveEdit(
                    fullName,
                    businessName,
                    businessSection,
                    stallNumber,
                    stallSize,
                    monthlyRental,
                    startDate,
                    paymentStatus,
                    orNumber,
                    penalty,
                    additionalCharge,
                    currentUserId);
            }
            else
            {
                SaveNew(
                    fullName,
                    businessName,
                    businessSection,
                    stallNumber,
                    stallSize,
                    monthlyRental,
                    startDate,
                    paymentStatus,
                    orNumber,
                    penalty,
                    additionalCharge,
                    currentUserId);
            }
        }


        // ===================== SAVE EDIT ===================== //
        private void SaveEdit(
            string fullName,
            string businessName,
            string businessSection,
            string stallNumber,
            string stallSize,
            decimal monthlyRental,
            string startDate,
            string paymentStatus,
            string orNumber,
            decimal penalty,
            decimal additionalCharge,
            long currentUserId)
        {
            var result =
                Database.UpdateProfiling(
                    currentSIN,
                    fullName,
                    businessName,
                    businessSection,
                    stallNumber,
                    stallSize,
                    monthlyRental,
                    startDate,
                    additionalCharge);


            if (result.Success)
            {
                Database.UpdatePaymentStatus(
                    currentSIN,
                    paymentStatus,
                    orNumber,
                    monthlyRental + penalty,
                    penalty,
                    currentUserId);


                Database.LogAudit(
                    "Update",
                    currentSIN,
                    currentUserId,
                    $"Updated profile for {fullName}, Status: {paymentStatus}" +
                    (paymentStatus == "Paid"
                        ? $", OR#: {orNumber}"
                        : ""));


                RecordSaved = true;


                MessageBox.Show(
                    "Record updated successfully!");


                this.Close();
            }
            else
            {
                MessageBox.Show(
                    result.ErrorMessage
                    ?? "Unknown error.");
            }
        }


        // ===================== SAVE NEW ===================== //
        private void SaveNew(
            string fullName,
            string businessName,
            string businessSection,
            string stallNumber,
            string stallSize,
            decimal monthlyRental,
            string startDate,
            string paymentStatus,
            string orNumber,
            decimal penalty,
            decimal additionalCharge,
            long currentUserId)
        {
            var result =
                Database.AddProfiling(
                    txtBIN.Text,
                    fullName,
                    businessName,
                    businessSection,
                    stallNumber,
                    stallSize,
                    monthlyRental,
                    startDate,
                    additionalCharge);


            if (result.Success)
            {
                Database.UpdatePaymentStatus(
                    txtBIN.Text,
                    paymentStatus,
                    orNumber,
                    monthlyRental + penalty,
                    penalty,
                    currentUserId);


                Database.LogAudit(
                    "Add",
                    txtBIN.Text,
                    currentUserId,
                    $"Added profile for {fullName}, Status: {paymentStatus}");


                RecordSaved = true;


                MessageBox.Show(
                    "Record added successfully!");


                ClearFields();
                AssignNewSIN();
            }
            else
            {
                MessageBox.Show(
                    result.ErrorMessage
                    ?? "Unknown error.");
            }
        }


        // ===================== HELPERS ===================== //
        private void AssignNewSIN()
        {
            txtBIN.Text =
                Database.GenerateUniqueSIN();
        }


        private void ClearFields()
        {
            txtFName.Clear();
            txtBName.Clear();

            cmbBSection.SelectedIndex = -1;

            txtSNumber.Clear();
            txtSSize.Clear();
            txtMRental.Clear();

            txtPenalty.Text = "0.00";

            txtAdditionalCharge.Text = "0.00";
            chkAdditional.Checked = false;

            cmbPaymentStatus.SelectedIndex = 0;

            dtpStartDate.Checked = false;
        }


        // ===================== NAVIGATION ===================== //
        private void button1_Click(
            object sender,
            EventArgs e)
        {
            if (isEditMode)
            {
                this.Close();
            }
            else
            {
                var dashboard =
                    new DashboardForm();

                dashboard.Show();

                this.Close();
            }
        }


        // ===================== EVENTS ===================== //
        private void txtMRental_Leave(
            object sender,
            EventArgs e)
        {
            if (decimal.TryParse(
                txtMRental.Text
                    .Replace(",", ""),
                out decimal value))
            {
                txtMRental.Text =
                    value.ToString("N2");
            }
        }


        // ===================== WINDOW SETTINGS ===================== //
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_NOCLOSE = 0x200;

                CreateParams cp =
                    base.CreateParams;

                cp.ClassStyle |= CS_NOCLOSE;

                return cp;
            }
        }


        // ===================== SECURITY ===================== //
        private void SetupSecurity()
        {
            TextBox[] protectedFields =
            {
                txtBIN,
                txtFName,
                txtBName,
                txtSNumber,
                txtSSize,
                txtAdditionalCharge
            };


            foreach (var field in protectedFields)
            {
                field.KeyDown += (s, e) =>
                {
                    if (e.Control &&
                        e.KeyCode == Keys.V)
                    {
                        e.SuppressKeyPress = true;
                    }
                };


                field.ContextMenuStrip =
                    new ContextMenuStrip();
            }
        }


        // ===================== RECORD SAVED ===================== //
        public bool RecordSaved
        {
            get;
            private set;
        } = false;


        // ===================== TOTAL AMOUNT DUE ===================== //
        private void UpdateTotalAmountDue(
            bool useLoadedPenalty = false)
        {
            // Don't perform calculations while the
            // edit form is still being populated.
            if (isLoading)
                return;


            decimal.TryParse(
                txtMRental.Text
                    .Replace(",", ""),
                out decimal rental);


            decimal.TryParse(
                txtAdditionalCharge.Text
                    .Replace(",", ""),
                out decimal additional);


            decimal penalty = 0;


            if (cmbPaymentStatus.SelectedItem?.ToString() == "Unverified")
            {
                txtPenalty.Text = "0.00";
                lblTotalDue.Text = "Pending verification";
                return;
            }

            if (isEditMode)
            {
                if (useLoadedPenalty)
                {
                    // Use the exact penalty that was
                    // displayed in ProfilingLists.
                    decimal.TryParse(
                        _editPenalty.Replace(",", ""),
                        out penalty);
                }
                else
                {
                    // Recalculate after the user changes
                    // something that affects the amount.
                    penalty =
                        Database.CalculateOutstandingPenalty(currentSIN);
                }
            }


            txtPenalty.Text =
                penalty.ToString("N2");


            decimal total =
                rental +
                penalty +
                (chkAdditional.Checked
                    ? additional
                    : 0);


            lblTotalDue.Text =
                total.ToString(
                    "C2",
                    new CultureInfo("en-PH"));
        }


        // ===================== UNUSED EVENT ===================== //
        private void txtBIN_TextChanged(
            object sender,
            EventArgs e)
        {
        }
    }
}