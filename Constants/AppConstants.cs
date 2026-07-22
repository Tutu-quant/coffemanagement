namespace CafeManagement.Constants
{
    public static class AppConstants
    {
        public const string AppName = "Quản lý Quán Café";
        public const string AppVersion = "1.0.0";

        // Pagination
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;

        // File Upload
        public const long MaxFileSize = 5242880; // 5MB
        public const string AllowedImageExtensions = ".jpg,.jpeg,.png,.gif";

        // Loyalty Points
        public const int PointsPerDollar = 1;
        public const int MinPointsToRedeem = 100;

        // Commission
        public const decimal CashierCommissionRate = 0.02m; // 2%
    }
}
