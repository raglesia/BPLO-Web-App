using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessPermitLicensingSystem.Models
{
    public class BillingReportModel
    {
        public string SIN { get; set; } =  string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessSection { get; set; } = string.Empty;
        public string StallNumber { get; set; } = string.Empty;
        public string StallSize { get; set; } = string.Empty;
        public decimal MonthlyRental { get; set; } = 0;
        public string PaymentStatus { get; set; } = "Unpaid";
        public decimal Penalty { get; set; } = 0;        
        public string StartDate { get; set; } = "";
        public decimal TotalDue => MonthlyRental + Penalty; 
        public decimal AdditionalCharge { get; set; } = 0;

        public int RowNumber { get; set; }


    }
}
