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

            // Sản phẩm bán chạy nhất
            ViewBag.TopProducts = _dataContext.Products.OrderByDescending(p => p.Sold).Take(5).ToList();

            // Khách mua nhiều nhất (tính theo số đơn)
            var topCustomers = _dataContext.Orders
                .GroupBy(o => o.UserName)
                .Select(g => new { UserName = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .AsEnumerable() // Chuyển sang xử lý Client-side sau khi đã lấy Top 5
                .Select(x => new KeyValuePair<string, int>(x.UserName, x.OrderCount))
                .ToList();
            ViewBag.TopCustomers = topCustomers;
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

        [HttpPost]
        public IActionResult GetDonutChartData()
        {
            try
            {
                var data = _dataContext.Products
                    .Include(p => p.Category)
                    .GroupBy(p => p.Category.Name)
                    .Select(g => new {
                        label = g.Key,
                        value = g.Count()
                    })
                    .ToList();
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
