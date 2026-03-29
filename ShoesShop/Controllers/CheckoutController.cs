using Microsoft.AspNetCore.Mvc;
using ShoesShop.Models;
using ShoesShop.Models.ViewModels;
using ShoesShop.Repository;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ShoesShop.Areas.Admin.Repository;
using Newtonsoft.Json;

namespace ShoesShop.Controllers
{
	public class CheckoutController : Controller
	{
		private readonly DataContext _dataContext;
		private readonly IEmailSender _emailSender;

		public CheckoutController(IEmailSender emailSender, DataContext context)
		{
			_dataContext = context;
			_emailSender = emailSender;
		}
		[HttpGet]
		public IActionResult Index()
		{
			// Lấy giỏ hàng từ session
			var cartItems = HttpContext.Session.GetJSon<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
			var shippingPriceCookie = Request.Cookies["ShippingPrice"];
			decimal shippingPrice = 0;
			if (shippingPriceCookie != null)
			{
				var shippingPriceJson = shippingPriceCookie;
				shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
			}
			if (cartItems.Count == 0)
			{
				TempData["error"] = "Giỏ hàng của bạn đang trống!";
				return RedirectToAction("Index", "Cart");
			}

			var cartVM = new CartItemViewModel
			{
				CartItems = cartItems,
				GrandTotal = cartItems.Sum(x => x.Price * x.Quantity),
				ShippingCost = shippingPrice
			};

            // Nếu user đã đăng nhập, lấy địa chỉ mặc định để auto-fill
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(userEmail))
            {
                var addresses = _dataContext.Addresses
                    .Where(a => a.UserName == userEmail)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenBy(a => a.Id)
                    .ToList();

                cartVM.Addresses = addresses;
                cartVM.DefaultAddress = addresses.FirstOrDefault();
            }

			return View("Checkout", cartVM);
		}
		[HttpPost]
		[Route("Cart/GetShipping")]
		public async Task<IActionResult> GetShipping(string tinh, string quan, string phuong)
		{
			try
			{
				// Kiểm tra dữ liệu đầu vào
				if (string.IsNullOrEmpty(tinh) || string.IsNullOrEmpty(quan) || string.IsNullOrEmpty(phuong))
				{
					return Json(new { success = false, message = "Vui lòng chọn đầy đủ Tỉnh/Thành, Quận/Huyện, Phường/Xã." });
				}

				// Tìm phí vận chuyển theo vị trí
				var existingShipping = await _dataContext.Shippings
					.FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

				decimal shippingPrice = existingShipping?.Price ?? 50000; // Nếu không có, mặc định 50k

				// Lưu vào cookie để tính tổng đơn hàng
				var shippingPriceJson = JsonConvert.SerializeObject(shippingPrice);
				var cookieOptions = new CookieOptions
				{
					HttpOnly = true,
					Secure = true, // đảm bảo chỉ gửi cookie qua HTTPS
					Expires = DateTimeOffset.UtcNow.AddMinutes(30)
				};

				Response.Cookies.Append("ShippingPrice", shippingPriceJson, cookieOptions);

				// Trả kết quả về AJAX
				return Json(new { success = true, price = shippingPrice });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding shipping price cookie: {ex.Message}");
				return Json(new { success = false, message = "Đã xảy ra lỗi trong khi xử lý." });
			}
		}
        [HttpPost]
        public IActionResult GetCoupon(int coupon_id)
        {
            try
            {
                var coupon = _dataContext.Coupons
                    .AsNoTracking()
                    .FirstOrDefault(c => c.Id == coupon_id && c.Status == 1 && c.Quantity > 0);

                if (coupon == null)
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });

                var now = DateTime.UtcNow;
                if (coupon.DateStart > now || coupon.DateEnd < now)
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });

                // Lưu vào Session
                HttpContext.Session.SetString("AppliedCouponId", coupon_id.ToString());
                HttpContext.Session.SetString("AppliedCouponCode", coupon.Name);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailableCoupons()
        {
            try
            {
                var now = DateTime.UtcNow;
                var coupons = await _dataContext.Coupons
                    .AsNoTracking()
                    .Where(c => c.Status == 1 &&
                                c.Quantity > 0 &&
                                c.DateStart <= now &&
                                c.DateEnd >= now)
                    .Select(c => new
                    {
                        id = c.Id,
                        code = c.Name,
                        description = c.Description ?? c.Name,
                        isPercentage = c.IsPercentage,
                        discountValue = c.DiscountValue,
                        minOrderAmount = c.MinOrderAmount ?? 0
                    })
                    .OrderBy(c => c.minOrderAmount)
                    .ToListAsync();

                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống." });
            }
        }
        [HttpPost]
        public IActionResult ApplyCoupon(int couponId)
        {
            // Chỉ cần trả success để JS biết là ok
            return Json(new { success = true });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
                return RedirectToAction("Login", "Account");

            var cartItems = HttpContext.Session.GetJSon<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            if (cartItems.Count == 0)
            {
                TempData["error"] = "Giỏ hàng trống, không thể thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var orderCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
            
            // Declare variables outside try block for scope access
            decimal grandTotal = 0;
            decimal discount = 0;
            string couponCode = null;

            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                // TÍNH TỔNG SẢN PHẨM
                grandTotal = cartItems.Sum(item => item.Price * item.Quantity);

                // TÍNH GIẢM GIÁ
                couponCode = string.IsNullOrWhiteSpace(model.CouponCode) ? null : model.CouponCode.Trim().ToUpper();

                if (!string.IsNullOrEmpty(couponCode))
                {
                    var coupon = await _dataContext.Coupons
                        .FirstOrDefaultAsync(c => c.Name.ToUpper() == couponCode);

                    if (coupon == null)
                    {
                        TempData["warning"] = "Mã giảm giá không tồn tại.";
                        couponCode = null;
                    }
                    else
                    {
                        var now = DateTime.Now;

                        // 1. Kiểm tra ngày hiệu lực
                        if (now < coupon.DateStart || now > coupon.DateEnd)
                        {
                            TempData["warning"] = "Mã giảm giá đã hết hạn hoặc chưa bắt đầu.";
                            couponCode = null;
                        }
                        // 2. Kiểm tra số lượng còn lại
                        else if (coupon.Quantity <= 0)
                        {
                            TempData["warning"] = "Mã giảm giá đã hết lượt sử dụng.";
                            couponCode = null;
                        }
                        // 3. Kiểm tra đã dùng 1 lần/người
                        else if (coupon.IsOneTimePerUser)
                        {
                            bool used = await _dataContext.Orders
                                .AnyAsync(o => o.UserName == userEmail && o.CouponCode == couponCode);

                            if (used)
                            {
                                TempData["warning"] = "Bạn đã sử dụng mã này rồi.";
                                couponCode = null;
                            }
                        }
                        // 4. Kiểm tra đơn tối thiểu
                        else if (grandTotal < (coupon.MinOrderAmount ?? 0))
                        {
                            TempData["warning"] = $"Đơn hàng cần tối thiểu {(coupon.MinOrderAmount ?? 0):N0} VNĐ để dùng mã này.";
                            couponCode = null;
                        }
                        else
                        {
                            // ÁP DỤNG GIẢM GIÁ
                            discount = coupon.IsPercentage
                                ? Math.Round(grandTotal * coupon.DiscountValue / 100)
                                : coupon.DiscountValue;

                            // GIẢM SỐ LƯỢNG COUPON
                            coupon.Quantity--;
                        }
                    }
                }

                // TẠO ĐƠN HÀNG
                var order = new OrderModel
                {
                    Orders_Code = orderCode,
                    UserName = userEmail,
                    Status = 0,              // Đã đặt
                    PaymentStatus = 0,       // Chờ thanh toán
                    PaymentMethod = model.PaymentMethod, // TPBank / MBBank / null
                    CreatedDate = DateTime.Now,

                    HouseNumber = model.HouseNumber,
                    City = model.tinhId,
                    District = model.quanId,
                    Ward = model.phuongId,
                    CityName = model.tinhName,
                    DistrictName = model.quanName,
                    WardName = model.phuongName,

                    ShippingCost = model.ShippingCost,
                    CouponCode = couponCode,
                    Discount = discount
                };

                _dataContext.Orders.Add(order);
                await _dataContext.SaveChangesAsync();

                // LƯU / CẬP NHẬT ĐỊA CHỈ MẶC ĐỊNH CHO USER
                // Lần đầu checkout: tạo mới và đặt IsDefault = true
                // Các lần sau: nếu địa chỉ mới khác địa chỉ mặc định hiện tại thì thêm bản ghi mới (user có thể có nhiều địa chỉ)
                var existingAddresses = await _dataContext.Addresses
                    .Where(a => a.UserName == userEmail)
                    .ToListAsync();

                bool isFirstAddress = !existingAddresses.Any();

                // Kiểm tra xem địa chỉ hiện tại đã tồn tại hay chưa (so sánh theo mã hành chính + số nhà)
                var sameAddress = existingAddresses.FirstOrDefault(a =>
                    a.HouseNumber == order.HouseNumber &&
                    a.City == order.City &&
                    a.District == order.District &&
                    a.Ward == order.Ward);

                if (sameAddress == null)
                {
                    // Nếu là địa chỉ mới, tạo bản ghi Address
                    var newAddress = new AddressModel
                    {
                        UserName = userEmail,
                        HouseNumber = order.HouseNumber,
                        City = order.City,
                        District = order.District,
                        Ward = order.Ward,
                        CityName = order.CityName,
                        DistrictName = order.DistrictName,
                        WardName = order.WardName,
                        // Lần đầu checkout => đặt mặc định
                        IsDefault = isFirstAddress
                    };

                    if (newAddress.IsDefault)
                    {
                        // Đảm bảo chỉ có 1 địa chỉ mặc định
                        foreach (var addr in existingAddresses)
                        {
                            addr.IsDefault = false;
                        }
                    }

                    _dataContext.Addresses.Add(newAddress);
                    await _dataContext.SaveChangesAsync();
                }

                // LƯU CHI TIẾT
                foreach (var item in cartItems)
                {
                    var details = new OrderDetails
                    {
                        OrderCode = orderCode,
                        ProductId = (int)item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        UserName = userEmail,
                        Size = item.Size
                    };
                    _dataContext.OrderDetails.Add(details);

                    var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        if (product.Quantity >= item.Quantity)
                        {
                            product.Quantity -= item.Quantity;
                            product.Sold += item.Quantity;
                        }
                        else
                        {
                            TempData["error"] = $"Sản phẩm {product.Name} không đủ hàng.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index", "Cart");
                        }
                    }
                }

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                try 
                {
                    await transaction.RollbackAsync();
                } 
                catch 
                {
                    // Transaction might be already completed or zombied
                }
                
                TempData["error"] = "Lỗi khi thanh toán: " + ex.Message;
                return RedirectToAction("History", "Account");
            }
            // End of Transaction Scome

            try
            {
                // XÓA GIỎ HÀNG
                HttpContext.Session.Remove("Cart");

                var totalAmount = grandTotal + model.ShippingCost - discount;

                // GỬI EMAIL (COD hoặc mặc định)
                var message = $@"
            <h2>Xin chào {userEmail},</h2>
            <p>Bạn đã đặt hàng thành công tại <strong>ShoesShop</strong>.</p>
            <p><b>Mã đơn hàng:</b> {orderCode}</p>
            <p><b>Tạm tính:</b> {grandTotal:N0} VNĐ</p>
            <p><b>Phí ship:</b> {model.ShippingCost:N0} VNĐ</p>
            {(discount > 0 ? $"<p><b>Giảm giá:</b> -{discount:N0} VNĐ{(couponCode != null ? $" (mã: {couponCode})" : "")}</p>" : "")}
            <p><b>TỔNG CỘNG:</b> <strong>{totalAmount:N0} VNĐ</strong></p>
            <p>Chúng tôi sẽ xác nhận và giao hàng trong thời gian sớm nhất.</p>
        ";

                await _emailSender.SendEmailAsync(userEmail, "Đặt hàng thành công", message, "order");

                TempData["success"] = "Thanh toán thành công! Vui lòng chờ duyệt đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }
             catch (Exception ex)
             {
                 TempData["success"] = "Đơn hàng đã tạo nhưng có lỗi gửi email/momo: " + ex.Message;
                 return RedirectToAction("History", "Account");
             }
        }
    }
	}

