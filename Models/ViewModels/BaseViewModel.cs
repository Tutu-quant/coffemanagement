// Models - ViewModels - Base ViewModel
namespace CafeManagement.Models.ViewModels
{
    public class BaseViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
