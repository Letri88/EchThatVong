public class CheckoutViewModel
{
    public string HouseNumber { get; set; }
    public string tinhId { get; set; }
    public string quanId { get; set; }
    public string phuongId { get; set; }
    public string tinhName { get; set; }
    public string quanName { get; set; }
    public string phuongName { get; set; }

    public string Note { get; set; }
    public bool SameAddress { get; set; }
    /// <summary>
    /// Phương thức thanh toán khách chọn (TPBank / MBBank / COD...)
    /// </summary>
    public string PaymentMethod { get; set; }
    public decimal ShippingCost { get; set; }
    public string CouponCode { get; set; }
    public decimal Discount { get; set; } = 0; // Số tiền giảm
}