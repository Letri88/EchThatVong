using System.ComponentModel.DataAnnotations;

namespace ShoesShop.Models.ViewModels
{
    public class ChatRequestViewModel
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public DateTime? BirthDate { get; set; }

        public string? Gender { get; set; }
    }
}

