using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Repository;

namespace ShoesShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Director")]
    public class DashboardController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnviroment;
        public DashboardController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _webHostEnviroment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var count_product = _dataContext.Products.Count();
            var count_order = _dataContext.Orders.Count();
            var count_user = _dataContext.Users.Count();
            var count_category = _dataContext.Categories.Count();
            ViewBag.CountProduct = count_product;
            ViewBag.CountOrder = count_order;
            ViewBag.CountUser = count_user;
            ViewBag.CountCategory = count_category;
            return View();
        }
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var endDate = DateTime.Today.AddDays(1).AddSeconds(-1); // cuối ngày hôm nay
                var startDate = DateTime.Today.AddDays(-29);            // 30 ngày

                var data = await _dataContext.Statisticals
                    .Where(s => s.DateCreated >= startDate && s.DateCreated <= endDate)
                    .OrderBy(s => s.DateCreated)
                    .Select(s => new
                    {
                        date = s.DateCreated.ToString("yyyy-MM-dd"),
                        sold = s.Sold,
                        quantity = s.Quantity,
                        revenue = s.Revenue,
                        profit = s.Profit
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Lỗi tải dữ liệu: " + ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetChartDataBySelect([FromForm] string startDate, [FromForm] string endDate)
        {
            try
            {
                if (!DateTime.TryParse(startDate, out DateTime start) ||
                    !DateTime.TryParse(endDate, out DateTime end))
                {
                    return Json(new { error = "Ngày không hợp lệ" });
                }

                // Đảm bảo endDate là cuối ngày
                end = end.Date.AddDays(1).AddSeconds(-1);

                var data = await _dataContext.Statisticals
                    .Where(s => s.DateCreated >= start && s.DateCreated <= end)
                    .OrderBy(s => s.DateCreated)
                    .Select(s => new
                    {
                        date = s.DateCreated.ToString("yyyy-MM-dd"),
                        sold = s.Sold,
                        quantity = s.Quantity,
                        revenue = s.Revenue,
                        profit = s.Profit
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Lỗi lọc dữ liệu: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult FilterData(DateTime? fromDate, DateTime? toDate)
        {
            var query = _dataContext.Statisticals.AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.DateCreated >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.DateCreated <= toDate);
            }

            var data = query
                .Select(s => new
                {
                    date = s.DateCreated.ToString("yyyy-MM-dd"),
                    sold = s.Sold,
                    quantity = s.Quantity,
                    revenue = s.Revenue,
                    profit = s.Profit
                })
                .ToList();
            return Json(data);
        }
    }
}
