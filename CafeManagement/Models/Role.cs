using System.ComponentModel.DataAnnotations;
namespace CafeManagement.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
        public virtual ICollection<User> Users { get; set; }
            = new List<User>();
    }
}