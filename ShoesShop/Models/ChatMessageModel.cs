using System.ComponentModel.DataAnnotations;

namespace ShoesShop.Models
{
    public class ChatMessageModel
    {
        [Key]
        public int Id { get; set; }

        public string ConversationId { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public string? UserName { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsFromAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ImagePath { get; set; }
    }
}

