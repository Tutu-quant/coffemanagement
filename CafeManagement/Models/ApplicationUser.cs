using Microsoft.AspNetCore.Identity;

namespace CafeManagement.Models
{
    // Mở rộng IdentityUser mặc định để thêm thông tin họ tên, ngày tạo...
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
