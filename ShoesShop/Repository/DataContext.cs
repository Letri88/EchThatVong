using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;

namespace ShoesShop.Repository
{
	public class DataContext : IdentityDbContext<AppUserModel>
	{
		public DataContext(DbContextOptions<DataContext>options):base(options) 
		{
		}
		public DbSet<BrandModel> Brands { get; set; }
		public DbSet<SliderModel> Sliders { get; set; }
		public DbSet<ProductModel> Products { get; set; }
		public DbSet<RatingModel> Ratings { get; set; }
		public DbSet<CategoryModel> Categories { get; set; }
		public DbSet<OrderModel> Orders { get; set; }
		public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<AboutModel> About { get; set; }
		public DbSet<WishlistModel> Wishlists { get; set; }
        public DbSet<ProductQuantityModel> ProductQuantities { get; set; }
        public DbSet<ShippingModel> Shippings { get; set; }
        public DbSet<CouponModel> Coupons { get; set; }
        public DbSet<StatisticalModel> Statisticals { get; set; }
        public DbSet<AddressModel> Addresses { get; set; }
        public DbSet<ChatMessageModel> ChatMessages { get; set; }
        public DbSet<BotChatMessageModel> BotChatMessages { get; set; }
        public DbSet<BotQuestionModel> BotQuestions { get; set; }
    }
}
