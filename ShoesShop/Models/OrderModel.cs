namespace ShoesShop.Models
{
	public class OrderModel
	{
		public int Id { get; set; }	
		public string Orders_Code { get; set; }
		public string UserName {  get; set; }
		public DateTime CreatedDate { get; set; }
		/// <summary>
		/// Trạng thái đơn hàng: 0 = Chờ xử lý, 1 = Đang chuẩn bị, 2 = Đang giao, 3 = Hoàn thành, 4 = Đã hủy
		/// </summary>
		public int Status {  get; set; }
        /// <summary>
        /// Trạng thái thanh toán: 0 = Chờ thanh toán, 1 = Đã thanh toán, 2 = Thanh toán thất bại
        /// </summary>
        public int PaymentStatus { get; set; }
        /// <summary>
        /// Phương thức thanh toán (vd: TPBank, MBBank, COD...)
        /// </summary>
        public string? PaymentMethod { get; set; }
		public decimal ShippingCost { get; set; }
        // Thêm địa chỉ giao hàng
        public string HouseNumber { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string CityName { get; set; }     
        public string DistrictName { get; set; } 
        public string WardName { get; set; }
        public string CouponCode { get; set; } // Mã coupon
        public decimal? Discount { get; set; }
        /// <summary>
        /// Mã giao dịch chuyển khoản (nếu khách nhập)
        /// </summary>
        public string? TransferCode { get; set; }
        /// <summary>
        /// Đường dẫn ảnh chứng từ thanh toán (upload)
        /// </summary>
        public string? TransferSlipPath { get; set; }
    }
}
