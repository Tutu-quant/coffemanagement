// Helpers - Price Helper
namespace CafeManagement.Helpers
{
    public static class PriceHelper
    {
        public static decimal CalculateTotal(decimal price, int quantity, decimal? discount = null)
        {
            decimal total = price * quantity;
            if (discount.HasValue && discount > 0)
            {
                total -= discount.Value;
            }
            return total;
        }

        public static string FormatPrice(decimal price)
        {
            return price.ToString("N0") + " đ";
        }
    }
}
