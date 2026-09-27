using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessPermitLicensingSystem.Models
{
    public class ImportRow
    {
        public string SIN { get; set; } = "";
        public string FullName { get; set; } = "";
        public string BusinessName { get; set; } = "";
        public string BusinessSection { get; set; } = "";
        public string StallNumber { get; set; } = "";
        public string StallSize { get; set; } = "";
        public string MonthlyRental { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string Penalty { get; set; } = "";
        public string AdditionalCharge { get; set; } = "";
        public string IsArchived { get; set; } = "0";
    }

        // ── VEHICLE PERMIT IMPORT ──────────────────────────────────────
        public class VehicleImportRow
        {
            public string CompanyName { get; set; } = "";
            public string DriverName { get; set; } = "";
            public string PlateNo { get; set; } = "";
            public string SECRegNo { get; set; } = "";
            public string DTINumber { get; set; } = "";
        }
    }
