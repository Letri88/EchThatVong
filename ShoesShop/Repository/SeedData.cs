using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;

namespace ShoesShop.Repository
{
	public class SeedData
	{
		public static void SeedingData(DataContext _context)
		{
		_context.Database.Migrate();
			if(!_context.Products.Any())
			{
				CategoryModel macbook = new CategoryModel { Name = "Apple", Slug = "apple", Description = "Macbook is large product in the world!", Status = 1 };
				CategoryModel Aspire = new CategoryModel { Name = "Aspire", Slug = "aspire", Description = "Acer is large brand in the world!", Status = 1 };
				BrandModel apple = new BrandModel { Name = "Apple", Slug = "samsung", Description = "Apple is large brand in the world!", Status = 1 };
				BrandModel acer = new BrandModel { Name = "Acer", Slug = "samsung", Description = "Apple is large brand in the world!", Status = 1 };
				_context.Products.AddRange(
					new ProductModel { Name = "Macbook", Slug = "macbook", Description = "Macbook is best", Image = "1.jpg", Category = macbook, Brand = apple, Price = 1200 },
					new ProductModel { Name = "AcerAspire7", Slug = "aspire", Description = "Acer is best", Image = "1.jpg", Category = Aspire, Brand = acer, Price = 1200 }
			);
				_context.SaveChanges();
			}
		}
	}
}
