using QuestPDF.Helpers;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Consistent theme and styling for Pharmacy PDF reports
    /// </summary>
    public static class PharmacyPdfTheme
    {
        // Fonts
        public static string PrimaryFont => "Helvetica";
        public static string SecondaryFont => "Arial";

        // Colors
        public static string PrimaryColor => Colors.Blue.Medium;      // Pharmacy blue
        public static string SecondaryColor => Colors.Grey.Medium;    // Grey
        public static string SuccessColor => Colors.Green.Medium;     // Green
        public static string WarningColor => Colors.Orange.Medium;    // Orange
        public static string DangerColor => Colors.Red.Medium;        // Red

        // Table colors
        public static class Table
        {
            public static string HeaderBackground => Colors.Grey.Lighten3;
            public static string HeaderTextColor => Colors.Grey.Darken4;
            public static string RowAlternateColor => Colors.Grey.Lighten5;
            public static string BorderColor => Colors.Grey.Lighten2;
            public static string SeparatorColor => Colors.Grey.Lighten1;
        }

        // Status colors
        public static string GetStatusColor(string status)
        {
            return status?.ToLower() switch
            {
                "instock" or "active" or "dispensed" => SuccessColor,
                "lowstock" or "pending" or "warning" => WarningColor,
                "outofstock" or "inactive" or "cancelled" => DangerColor,
                _ => SecondaryColor
            };
        }

        // Quantity color based on stock level
        public static string GetQuantityColor(int quantity, int minStock)
        {
            if (quantity == 0) return DangerColor;
            if (quantity <= minStock) return WarningColor;
            return Colors.Black;
        }

        // Expiry date color based on days until expiry
        public static string GetExpiryColor(DateTime? expiryDate)
        {
            if (!expiryDate.HasValue) return SecondaryColor;

            var daysUntilExpiry = (expiryDate.Value.Date - DateTime.Now.Date).Days;

            if (daysUntilExpiry < 0) return DangerColor;      // Expired
            if (daysUntilExpiry < 30) return WarningColor;    // Expiring soon
            return Colors.Black;                              // OK
        }
    }
}
