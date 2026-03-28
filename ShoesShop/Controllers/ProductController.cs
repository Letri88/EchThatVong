using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;
using ShoesShop.Models.ViewModels;
using ShoesShop.Repository;

namespace ShoesShop.Controllers

{
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        public ProductController(DataContext context)
        {
            _dataContext = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Search(string searchTerm)
        {
            var products = await _dataContext.Products
            .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
            .ToListAsync();

            ViewBag.Keyword = searchTerm;

            return View(products);
        }
		public async Task<IActionResult> Details(int Id)
		{
			if (Id <= 0)
				return RedirectToAction("Index");

			var productsById = await _dataContext.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == Id);
			if (productsById == null)
			{
				TempData["error"] = "Sản phẩm không tồn tại!";
				return RedirectToAction("Index");
			}

            // Lấy danh sách đánh giá cho sản phẩm
            var reviews = await _dataContext.Ratings
                .Where(r => r.ProductId == Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            double averageRating = 0;
            int totalReviews = reviews.Count;
            if (totalReviews > 0)
            {
                averageRating = reviews.Average(r => r.Stars);
            }

            // Kiểm tra quyền được đánh giá: chỉ khách đã mua và đơn trạng thái hoàn thành (Status = 3)
            bool canReview = false;
            string? userEmail = User?.Identity?.IsAuthenticated == true
                ? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                : null;

            if (!string.IsNullOrEmpty(userEmail))
            {
                canReview = await _dataContext.Orders
                    .Where(o => o.UserName == userEmail && o.Status == 3)
                    .Join(_dataContext.OrderDetails,
                          o => o.Orders_Code,
                          d => d.OrderCode,
                          (o, d) => new { o, d })
                    .AnyAsync(x => x.d.ProductId == Id);

                // Nếu đã từng đánh giá sản phẩm này rồi thì không cho đánh giá lại
                if (canReview)
                {
                    bool alreadyReviewed = await _dataContext.Ratings
                        .AnyAsync(r => r.ProductId == Id && r.UserName == userEmail);
                    if (alreadyReviewed)
                    {
                        canReview = false;
                    }
                }
            }

			var relatedProducts = await _dataContext.Products
				.Where(p => p.CategoryId == productsById.CategoryId && p.Id != productsById.Id)
				.Take(4)
				.ToListAsync();

			ViewBag.RelatedProducts = relatedProducts;
			var viewModel = new ProductDetailsViewModel
			{
				ProductDetails = productsById,
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                CanReview = canReview,
                Reviews = reviews
			};
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CommentProduct(int productId, int stars, string comment)
		{
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                TempData["error"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
                return RedirectToAction("Login", "Account");
            }

            if (stars < 1 || stars > 5)
            {
                TempData["error"] = "Số sao không hợp lệ.";
                return RedirectToAction("Details", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["error"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToAction("Details", new { id = productId });
            }

            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Không xác định được thông tin tài khoản.";
                return RedirectToAction("Details", new { id = productId });
            }

            // Kiểm tra đã mua và đơn đã hoàn thành
            bool canReview = await _dataContext.Orders
                .Where(o => o.UserName == userEmail && o.Status == 3)
                .Join(_dataContext.OrderDetails,
                      o => o.Orders_Code,
                      d => d.OrderCode,
                      (o, d) => new { o, d })
                .AnyAsync(x => x.d.ProductId == productId);

            if (!canReview)
            {
                TempData["error"] = "Chỉ những khách đã mua và đơn hàng đã giao thành công mới được đánh giá.";
                return RedirectToAction("Details", new { id = productId });
            }

            // Không cho đánh giá trùng
            bool alreadyReviewed = await _dataContext.Ratings
                .AnyAsync(r => r.ProductId == productId && r.UserName == userEmail);

            if (alreadyReviewed)
            {
                TempData["error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Details", new { id = productId });
            }

            var ratingEntity = new RatingModel
            {
                ProductId = productId,
                Stars = stars,
                Comment = comment.Trim(),
                UserId = userId,
                UserName = userEmail,
                CreatedAt = DateTime.Now
            };

            _dataContext.Ratings.Add(ratingEntity);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm đánh giá thành công.";

            return RedirectToAction("Details", new { id = productId });
		}
	}
}