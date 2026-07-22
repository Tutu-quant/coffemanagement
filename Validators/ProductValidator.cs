// Validators - Product Validator
namespace CafeManagement.Validators
{
    public class ProductValidator
    {
        public bool Validate(string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (price <= 0)
                return false;

            return true;
        }
    }
}
