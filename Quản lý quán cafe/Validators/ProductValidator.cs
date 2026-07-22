namespace Quản_lý_quán_cafe.Validators
{
    /// <summary>
    /// Product validation helper class
    /// </summary>
    public class ProductValidator
    {
        /// <summary>
        /// Validates image file format and size
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            if (file == null)
                return (true, string.Empty);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Định dạng ảnh không hợp lệ. Chỉ chấp nhận: JPG, JPEG, PNG, WebP");
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return (false, "Kích thước ảnh không được vượt quá 5MB");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates product name
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Tên sản phẩm là bắt buộc");
            }

            if (name.Length > 100)
            {
                return (false, "Tên sản phẩm phải có tối đa 100 ký tự");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates product price
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidatePrice(decimal price)
        {
            if (price <= 0)
            {
                return (false, "Giá sản phẩm phải lớn hơn 0");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates category
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return (false, "Danh mục là bắt buộc");
            }

            return (true, string.Empty);
        }
    }
}
