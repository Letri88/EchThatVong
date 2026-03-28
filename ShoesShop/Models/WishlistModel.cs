using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
	public class WishlistModel
	{
		public int Id { get; set; }	
		public int ProductId { get; set; }
		public string UserId { get; set; }
		[ForeignKey("ProductId")]
		public ProductModel Product { get; set; }
	}
}
