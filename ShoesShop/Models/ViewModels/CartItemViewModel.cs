using System.Collections.Generic;
using ShoesShop.Models;

namespace ShoesShop.Models.ViewModels
{
	public class CartItemViewModel
	{
		public List<CartItemModel> CartItems { get;	set; }
		public decimal GrandTotal { get; set; }
		public decimal ShippingCost { get; set; }
        // Danh sách địa chỉ đã lưu của user (nếu có)
        public List<AddressModel>? Addresses { get; set; }
        // Địa chỉ mặc định dùng để auto-fill lần đầu
        public AddressModel? DefaultAddress { get; set; }
	}
}
