namespace ShoesShop.Models
{
	public class CartItemModel
	{
		public int ProductId {  get; set; }
		public string ProductName { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
		public decimal Total {
			get {  return Quantity*Price; }
				}
		public string Image {  get; set; }
		public string Size { get; set; }
		public CartItemModel() 
		{
		}
		public CartItemModel(ProductModel product, string size)
		{
			ProductId = product.Id;
			ProductName = product.Name;
			Price = product.Price;
			Quantity = 1;
			Image = product.Image;
			Size = size;
		}
	}
}
