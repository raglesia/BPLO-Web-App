using System;
using System.Collections.Generic;
using System.Text;

    namespace BusinessPermitLicensingSystem.Helpers
    {
    public static class InputValidator
    {
        // Full Name: letters, optional dot, optional space
        public static void AllowOnlyLetters(KeyPressEventArgs e, bool allowDot = true, bool allowSpace = true)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsLetter(e.KeyChar)) return;
            if (allowDot && e.KeyChar == '.') return;
            if (allowSpace && e.KeyChar == ' ') return;

            e.Handled = true;
        }

        // Business Name: letters, digits, dot, comma, space
        public static void AllowLettersDigitsDotCommaSpace(KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsLetterOrDigit(e.KeyChar)) return;
            if (e.KeyChar == '.' || e.KeyChar == ',' || e.KeyChar == ' ') return;

            e.Handled = true;
        }

        // Stall Number: digits only
        public static void AllowOnlyDigits(KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;

            e.Handled = true;
        }

        // Stall Size / Monthly Rental: digits + dot
        public static void AllowDecimalNumbers(KeyPressEventArgs e, TextBox tb)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;

            // Allow one dot only
            if (e.KeyChar == '.' && !tb.Text.Contains(".")) return;

            e.Handled = true;
        }
        public static void AllowDigitsAndComma(KeyPressEventArgs e)
        {
            // ✅ Allow digits, comma, and backspace
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '\b')
                e.Handled = true;
        }
    }

}
