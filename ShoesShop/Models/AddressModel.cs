using System.ComponentModel.DataAnnotations;

namespace ShoesShop.Models
{
    public class AddressModel
    {
        public int Id { get; set; }

        // Lưu theo email (UserName) tương tự OrderModel để đơn giản hoá tích hợp
        [Required]
        public string UserName { get; set; }

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? HouseNumber { get; set; }

        // Mã hành chính
        public string? City { get; set; }

        public string? District { get; set; }

        public string? Ward { get; set; }

        // Tên hiển thị
        public string? CityName { get; set; }

        public string? DistrictName { get; set; }

        public string? WardName { get; set; }

        public bool IsDefault { get; set; }
    }
}

