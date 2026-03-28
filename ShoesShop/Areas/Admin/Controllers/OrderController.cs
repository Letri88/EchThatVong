using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;
using ShoesShop.Repository;

namespace ShoesShop.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize(Roles = "Admin,Director")]
    public class OrderController : Controller
    {
		private readonly DataContext _dataContext;
		public OrderController(DataContext context)
		{
			_dataContext = context;
		}
        public async Task<IActionResult> Index(int pg = 1)
        {
            const int pageSize = 10;

            if (pg < 1)
                pg = 1;

            // Lấy danh sách đơn hàng sắp xếp giảm dần theo Id
            var orders = await _dataContext.Orders
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            int recsCount = orders.Count;

            // Tạo đối tượng phân trang
            var pager = new Paginate(recsCount, pg, pageSize);

            int recSkip = (pg - 1) * pageSize;
            var data = orders.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;

            return View(data);
        }
        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.Orders_Code == ordercode);
            if (order == null)
                return NotFound();

            int oldStatus = order.Status;

            // Nếu trạng thái không thay đổi thì không làm gì
            if (oldStatus == status)
                return Ok(new { success = true, message = "Không có thay đổi trạng thái" });

            order.Status = status;
            // Nếu đơn được chuyển sang Hoàn thành mà trước đó chưa đánh dấu thanh toán, tự set đã thanh toán (phù hợp COD/nhận tiền đủ)
            if (status == 3 && order.PaymentStatus == 0)
            {
                order.PaymentStatus = 1;
            }

            // Lấy chi tiết đơn hàng
            var DetailsOrder = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == order.Orders_Code)
                .ToListAsync();

            int quantity = DetailsOrder.Count;
            int sold = DetailsOrder.Sum(x => x.Quantity);
            decimal revenue = DetailsOrder.Sum(x => x.Quantity * x.Product.Price);
            decimal profit = DetailsOrder.Sum(x => x.Quantity * (x.Product.Price - x.Product.Capital));

            // Lấy thống kê theo ngày
            var statisticalModel = await _dataContext.Statisticals
                .FirstOrDefaultAsync(s => s.DateCreated.Date == order.CreatedDate.Date);

            // Quy ước mới:
            // - Chỉ tính DOANH THU khi đơn ở trạng thái Hoàn thành (Status = 3)
            // - Khi chuyển từ trạng thái KHÔNG PHẢI Hoàn thành -> Hoàn thành: cộng thống kê
            // - Khi chuyển từ Hoàn thành -> trạng thái khác: trừ thống kê
            if (oldStatus != 3 && status == 3)
            {
                if (statisticalModel == null)
                {
                    statisticalModel = new StatisticalModel
                    {
                        DateCreated = order.CreatedDate.Date,
                        Quantity = quantity,
                        Sold = sold,
                        Revenue = revenue,
                        Profit = profit
                    };
                    _dataContext.Statisticals.Add(statisticalModel);
                }
                else
                {
                    statisticalModel.Quantity += quantity;
                    statisticalModel.Sold += sold;
                    statisticalModel.Revenue += revenue;
                    statisticalModel.Profit += profit;

                    _dataContext.Statisticals.Update(statisticalModel);
                }
            }
            else if (oldStatus == 3 && status != 3)
            {
                if (statisticalModel != null)
                {
                    statisticalModel.Quantity -= quantity;
                    statisticalModel.Sold -= sold;
                    statisticalModel.Revenue -= revenue;
                    statisticalModel.Profit -= profit;

                    _dataContext.Statisticals.Update(statisticalModel);
                }
            }

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công" });
            }
            catch
            {
                return StatusCode(500, "Có lỗi khi cập nhật đơn hàng");
            }
        }

        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var details = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == ordercode)
                .ToListAsync();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.Orders_Code == ordercode);

            if (order == null) return NotFound();

            ViewBag.Status = order.Status;
            ViewBag.PaymentStatus = order.PaymentStatus;
            ViewBag.PaymentMethod = order.PaymentMethod;
            ViewBag.TransferCode = order.TransferCode;
            ViewBag.TransferSlipPath = order.TransferSlipPath;
            ViewBag.OrderCode = order.Orders_Code;

            // TÍNH TOTAL ĐÚNG
            decimal productTotal = details.Sum(x => x.Price * x.Quantity);
            decimal shipping = order.ShippingCost;
            decimal discount = order.Discount ?? 0m;
            ViewBag.FinalTotal = productTotal + shipping - discount;

            return View(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Order/ConfirmPayment")]
        public async Task<IActionResult> ConfirmPayment(string ordercode)
        {
            if (string.IsNullOrWhiteSpace(ordercode))
            {
                TempData["error"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("Index");
            }

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.Orders_Code == ordercode);

            if (order == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            order.PaymentStatus = 1;
            _dataContext.Orders.Update(order);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Đã xác nhận thanh toán cho đơn hàng.";
            return RedirectToAction("ViewOrder", new { ordercode });
        }


    }
}
