using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
    public class CouponModel
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage ="Yêu cầu nhập tên")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập mô tả")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập số lượng")]
        public int Quantity { get; set; } 
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public int Status { get; set; }
        
        [Required(ErrorMessage = "Chọn loại giảm giá")]
        public bool IsPercentage { get; set; } = false; // true = %, false = cố định

        [Required(ErrorMessage = "Nhập giá trị giảm")]
        [Range(0.01, 1000000, ErrorMessage = "Giá trị từ 0.01 đến 1.000.000")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; } // 10.00 (%) hoặc 50000 (VNĐ)

        [Range(0, 10000000, ErrorMessage = "Đơn tối thiểu từ 0 đến 10 triệu")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; } // Đơn hàng tối thiểu để dùng


        public bool IsOneTimePerUser { get; set; } = false;

    }
}
