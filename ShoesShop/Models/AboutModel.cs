using ShoesShop.Repository.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
    public class AboutModel
    {
        [Key]

        [Required(ErrorMessage="Yêu cầu tiêu đề website")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu cầu địa chỉ")]
        public string Map { get; set; }
        [Required(ErrorMessage = "Yêu cầu số điện thoại liên hệ")]
        public string Phone { get; set; }
        [Required(ErrorMessage = "Yêu cầu email liên hệ")]
        public string Email { get; set; }
        public string Description { get; set; }
        public string LogoImage { get; set; }
        [NotMapped]
        [FileExtension]
        public IFormFile? ImageUpload { get; set; }
    }
}
