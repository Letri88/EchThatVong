using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesShop.Models
{
	public class OrderDetails
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public string OrderCode {  get; set; }
		public int ProductId {  get; set; }
        [Column(TypeName = "decimal(18,2)")] 
        public decimal Price { get; set; }
        public int Quantity {  get; set; }
		public string? Size { get; set; }
		public ProductModel Product { get; set; }
	}
}
