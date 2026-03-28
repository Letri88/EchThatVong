using System.ComponentModel.DataAnnotations;

namespace ShoesShop.Models
{
    public class BotChatMessageModel
    {
        [Key]
        public int Id { get; set; }

        public string ConversationId { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public string? UserName { get; set; }

        public string Message { get; set; } = string.Empty;

        // user | bot
        public string Role { get; set; } = "user";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

