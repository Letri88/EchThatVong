using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Repository;

namespace ShoesShop.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly DataContext _context;

        public OrderTrackingController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                TempData["error"] = "Vui lòng nhập mã đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Orders_Code == orderCode.Trim());

            if (order == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng với mã này.";
                return RedirectToAction(nameof(Index));
            }

            return View("Result", order);
        }
    }
}

