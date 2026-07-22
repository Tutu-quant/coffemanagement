// Helpers - Image Helper
namespace CafeManagement.Helpers
{
    public static class ImageHelper
    {
        public static string GetImagePath(string fileName)
        {
            return $"/uploads/{fileName}";
        }

        public static bool IsValidImageExtension(string fileName)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(fileName).ToLower();
            return validExtensions.Contains(extension);
        }
    }
}
