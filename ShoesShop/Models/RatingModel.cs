using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
	public class RatingModel
	{
		[Key]
		public int Id { get; set; }
		
		public int ProductId {  get; set; }
		
        // Số sao (1-5)
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        public int Stars { get; set; }

        // Nội dung bình luận
        [Required(ErrorMessage ="Yêu cầu nhập đánh giá")]
		public string Comment {  get; set; }

        // Gắn với user trong hệ thống
        public string? UserId { get; set; }
        public string? UserName { get; set; } // email hoặc tên đăng nhập

        // Thuộc tính cũ, giữ lại nếu đã tồn tại cột trong DB
        public string? Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("ProductId")]
		public ProductModel Product { get; set; }


	}
}
