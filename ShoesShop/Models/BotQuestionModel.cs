using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
    public class BotQuestionModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi hiển thị cho người dùng")]
        [Display(Name = "Ngôn từ người dùng gửi (VD: Xin chào)")]
        public string? UserMessage { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập câu trả lời của Bot")]
        [Display(Name = "Bot trả lời (VD: Chào bạn, bạn muốn mua gì?)")]
        public string? BotReply { get; set; }

        [Display(Name = "Câu hỏi cha")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual BotQuestionModel? ParentQuestion { get; set; }

        [Display(Name = "Hiển thị lựa chọn Danh mục?")]
        public bool ShowCategoryOptions { get; set; } = false;

        [Display(Name = "Thứ tự hiển thị")]
        public int OrderIndex { get; set; } = 0;

        [Display(Name = "Trạng thái hiển thị")]
        public int Status { get; set; } = 1; // 1: Hiển thị, 0: Ẩn
    }
}
