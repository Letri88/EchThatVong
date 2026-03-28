using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ShoesShop.Models;

namespace ShoesShop.Models.ViewModels
{
	public class ProductDetailsViewModel
	{
		public ProductModel ProductDetails { get; set; }
		
        // Thông tin phục vụ hiển thị đánh giá
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool CanReview { get; set; }
        public List<RatingModel> Reviews { get; set; } = new List<RatingModel>();
	}
}
