using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Areas.Admin.Repository;
using ShoesShop.Models;
using ShoesShop.Repository;
using ShoesShop.Services;



var builder = WebApplication.CreateBuilder(args);

// ========= DATABASE CONFIG =========
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectedDb"));
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

// ========= MVC =========
builder.Services.AddControllersWithViews();

// ========= HTTP CLIENTS / SERVICES =========
builder.Services.AddHttpClient<GeminiChatService>();

// ========= SESSION =========
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// ========= IDENTITY =========
builder.Services.AddIdentity<AppUserModel, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<DataContext>()
.AddDefaultTokenProviders();

// ========= GOOGLE LOGIN =========
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["GoogleKeys:ClientId"]!;
        googleOptions.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"]!;
        googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    });

var app = builder.Build();
// =================== FIX LỖI LOG BỊ LOCK TRÊN SOMEE.COM ===================
bool isSomee = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"))
               && Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")!.Contains("somee", StringComparison.OrdinalIgnoreCase)
               || Environment.GetEnvironmentVariable("SOMEE") != null;

if (isSomee)
{
    // Xóa hết các logger mặc định (trong đó có Console Logger ghi file stdout_*.log)
    builder.Logging.ClearProviders();
    // Chỉ để lại Console thuần, Somee sẽ tự lưu thành file stdout_*.log cho bạn đọc
    builder.Logging.AddConsole();

    // Tắt luôn việc ASP.NET Core tự tạo thư mục logs và ghi file
    builder.Logging.SetMinimumLevel(LogLevel.Warning); // tùy bạn, có thể để Information cũng được
}

// ========= ERROR PAGE =========
app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");

// ========= SESSION =========
app.UseSession();

// ========= ERROR HANDLING =========
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ========= ROUTES =========
app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Product}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "category",
    pattern: "/category/{Slug?}",
    defaults: new { controller = "Category", action = "Index" });

app.MapControllerRoute(
    name: "brand",
    pattern: "/brand/{Slug?}",
    defaults: new { controller = "Brand", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ========= SEED DATA — ONLY RUN IN DEVELOPMENT =========
#if DEBUG
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    SeedData.SeedingData(context);
}
#endif

app.Run();
