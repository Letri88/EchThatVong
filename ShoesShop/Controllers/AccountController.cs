using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Areas.Admin.Repository;
using ShoesShop.Models;
using ShoesShop.Models.ViewModels;
using ShoesShop.Repository;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ShoesShop.Controllers
{
	public class AccountController : Controller
	{
		private UserManager<AppUserModel> _userManager;
		private SignInManager<AppUserModel> _signInManager;
		private readonly DataContext _dataContext;
		private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

		public AccountController(SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManager, DataContext context, IEmailSender emailSender, RoleManager<IdentityRole> roleManager)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_dataContext = context;
			_emailSender = emailSender;
            _roleManager = roleManager;
		}


		public IActionResult Login(string returnUrl)
		{
			return View(new LoginViewModel { ReturnUrl = returnUrl });
		}
		public async Task<IActionResult> UpdateAccount()
		{
			if ((bool)!User.Identity?.IsAuthenticated)
			{
				return RedirectToAction("Login", "Account");
			}
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
			if (user == null)
			{
				return NotFound();
			}
			return View(user);
		}
		[HttpPost]
		public async Task<IActionResult> UpdateInfo(AppUserModel model)
		{
			if ((bool)!User.Identity?.IsAuthenticated)
			{
				return RedirectToAction("Login", "Account");
			}
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			//var userEmail = User.FindFirstValue(ClaimTypes.Email);
			var userById = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (userById == null)
			{
				return NotFound();
			}
			else
			{
				userById.PhoneNumber = model.PhoneNumber;
				var result = await _userManager.UpdateAsync(userById);
				if (result.Succeeded)
				{
					TempData["success"] = "Cập nhật thông tin thành công";
				}
				else
				{
					TempData["error"] = "Có lỗi khi cập nhật thông tin";
				}
			}
			return RedirectToAction("UpdateAccount", "Account");

		}
		public async Task<IActionResult> ForgetPassword(string returnUrl)
		{
			return View();
		}
		// GET: Hiển thị form reset
		[HttpGet]
		public async Task<IActionResult> NewPassword(string email, string token)
		{
			if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
			{
				TempData["error"] = "Link không hợp lệ.";
				return RedirectToAction("ForgetPassword");
			}

			// Tìm user bằng email + token từ URL
			var user = await _userManager.Users
				.FirstOrDefaultAsync(u => u.Email == email && u.Token == token);

			if (user == null)
			{
				TempData["error"] = "Email hoặc Token không đúng.";
				return RedirectToAction("ForgetPassword");
			}

			// Truyền dữ liệu qua ViewBag hoặc Model
			ViewBag.Email = email;
			ViewBag.Token = token;

			return View();
		}

		// POST: Cập nhật mật khẩu
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateNewPassword(string email, string token, string password, string confirmPassword)
		{
			if (string.IsNullOrEmpty(password) || password != confirmPassword)
			{
				TempData["error"] = "Mật khẩu không khớp hoặc trống.";
				return RedirectToAction("NewPassword", new { email, token });
			}

			var user = await _userManager.Users
				.FirstOrDefaultAsync(u => u.Email == email && u.Token == token);

			if (user == null)
			{
				TempData["error"] = "Token đã hết hạn hoặc không hợp lệ.";
				return RedirectToAction("ForgetPassword");
			}

			// Dùng UserManager để hash mật khẩu đúng cách
			var passwordHasher = _userManager.PasswordHasher;
			user.PasswordHash = passwordHasher.HashPassword(user, password);

			// Xóa token cũ (one-time use)
			user.Token = null; // hoặc set expired

			await _userManager.UpdateAsync(user);

			TempData["success"] = "Cập nhật mật khẩu thành công!";
			return RedirectToAction("Login");
		}
		[HttpPost]
		public async Task<IActionResult> SendMailForgetPass(AppUserModel user)
		{
			// 1️⃣ Kiểm tra email có tồn tại trong hệ thống hay không
			var checkmail = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
			if (checkmail == null)
			{
				TempData["error"] = "Không tìm thấy email khôi phục";
				return RedirectToAction("ForgetPassword", "Account");
			}
			else
			{
				// 2️⃣ Sinh token ngẫu nhiên để xác thực link khôi phục
				string token = Guid.NewGuid().ToString();
				checkmail.Token = token; // Lưu token này trong DB để xác minh khi người dùng nhấn link

				_dataContext.Update(checkmail);
				await _dataContext.SaveChangesAsync();

				// 3️⃣ Gửi email khôi phục
				var receiver = checkmail.Email;
				var subject = "Thay đổi mật khẩu " + checkmail.Email;
				var message = "Nhấn vào link để thay đổi mật khẩu của bạn " +
					"<a href='" + $"{Request.Scheme}://{Request.Host}/Account/NewPassword?email=" + checkmail.Email + "&token=" + token + "'>Tại đây</a>";

				await _emailSender.SendEmailAsync(receiver, subject, message, "reset");
			}

			// 4️⃣ Thông báo kết quả
			TempData["success"] = "Email đã được gửi để khôi phục mật khẩu";
			return RedirectToAction("ForgetPassword", "Account");
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel loginVM)
		{
			if (!ModelState.IsValid)
				return View(loginVM);

			// Hỗ trợ login bằng email
			var user = await _userManager.FindByEmailAsync(loginVM.Username);
			if (user != null)
			{
				loginVM.Username = user.UserName;
			}
			else
			{
				// Nếu không tìm thấy bằng email → thử bằng username
				user = await _userManager.FindByNameAsync(loginVM.Username);
			}

			if (user == null)
			{
				ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
				return View(loginVM);
			}

			// Kiểm tra user có password không (user Google thì không có)
			var hasPassword = await _userManager.HasPasswordAsync(user);
			if (!hasPassword)
			{
				ModelState.AddModelError("", "Tài khoản này chỉ có thể đăng nhập bằng Google.");
				return View(loginVM);
			}

			var result = await _signInManager.PasswordSignInAsync(
				loginVM.Username,
				loginVM.Password,
				false,
				false);

			if (result.Succeeded)
			{
				return Redirect(loginVM.ReturnUrl ?? "/");
			}

			ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
			return View(loginVM);
		}
		public IActionResult Create()
		{
			return View();
		}
		public async Task<IActionResult> History()
		{
			if ((bool)!User.Identity?.IsAuthenticated)
			{
				return RedirectToAction("Login", "Account");
			}
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var Orders = await _dataContext.Orders
                .Where(od => od.UserName == userEmail)
                .OrderByDescending(od => od.Id)
                .ToListAsync();
			ViewBag.UserEmail = userEmail;
			return View(Orders);
		}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTransferProof(string orderCode, string transferCode, IFormFile transferSlip)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(orderCode))
            {
                TempData["error"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("History");
            }

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.Orders_Code == orderCode);

            if (order == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            // Chỉ cho phép cập nhật nếu đơn chưa thanh toán
            if (order.PaymentStatus == 1)
            {
                TempData["success"] = "Đơn hàng này đã được xác nhận thanh toán.";
                return RedirectToAction("History");
            }

            if (!string.IsNullOrWhiteSpace(transferCode))
            {
                order.TransferCode = transferCode.Trim();
            }

            if (transferSlip != null && transferSlip.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "transfers");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{order.Orders_Code}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(transferSlip.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await transferSlip.CopyToAsync(stream);
                }

                order.TransferSlipPath = $"/uploads/transfers/{fileName}";
            }

            _dataContext.Orders.Update(order);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Đã gửi thông tin chuyển khoản. Vui lòng chờ cửa hàng xác nhận.";
            return RedirectToAction("History");
        }
		[HttpGet]
		public async Task<IActionResult> CancelOrder(string ordercode)
		{
			// Kiểm tra đăng nhập
			if (!User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Login", "Account");
			}

			try
			{
				var order = await _dataContext.Orders
					.FirstOrDefaultAsync(o => o.Orders_Code == ordercode);

				if (order == null)
				{
					return NotFound("Không tìm thấy đơn hàng.");
				}

				if (order.Status == 3)
				{
					TempData["Message"] = "Đơn hàng này đã bị hủy trước đó.";
					return RedirectToAction("History", "Account");
				}

				// Cập nhật trạng thái
				order.Status = 3;
				_dataContext.Orders.Update(order);
				await _dataContext.SaveChangesAsync();

				TempData["Message"] = "Hủy đơn hàng thành công!";
			}
			catch (Exception)
			{
				TempData["Message"] = "Có lỗi xảy ra khi hủy đơn hàng.";
			}

			return RedirectToAction("History", "Account");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserModel user)
		{
			if (ModelState.IsValid)
			{
				AppUserModel newUser = new AppUserModel { UserName = user.Username, Email = user.Email };
				IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);
				if (result.Succeeded)
				{
                    // Check if role exists, if not create it (Safety check)
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    await _userManager.AddToRoleAsync(newUser, "User");
					TempData["success"] = "Tạo người dùng thành công";
					return Redirect("/account/login");
				}
				foreach (IdentityError error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}
			return View(user);
		}
		public async Task<IActionResult> Logout(string returnURl = "/")
		{
			await _signInManager.SignOutAsync();
			await HttpContext.SignOutAsync();
			return Redirect(returnURl);

		}
		public async Task LoginByGoogle()
		{
			await HttpContext.ChallengeAsync(
				GoogleDefaults.AuthenticationScheme,
				new AuthenticationProperties
				{
					RedirectUri = Url.Action("GoogleResponse")
				});
		}

		public async Task<IActionResult> GoogleResponse()
		{
			var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
			if (!result.Succeeded)
			{
				return RedirectToAction("Login");
			}

			var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
			var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

			if (email == null)
				return RedirectToAction("Login");

			// Lấy phần trước @ làm username
			var username = email.Split('@')[0];

			// Kiểm tra user tồn tại chưa
			var existingUser = await _userManager.FindByEmailAsync(email);

			if (existingUser == null)
			{
				var newUser = new AppUserModel
				{
					UserName = username,
					Email = email
				};

				// Tạo user KHÔNG MẬT KHẨU
				var createResult = await _userManager.CreateAsync(newUser);

				if (!createResult.Succeeded)
				{
					TempData["error"] = "Đăng ký bằng Google thất bại";
					return RedirectToAction("Login");
				}

				// Đăng nhập luôn
				await _signInManager.SignInAsync(newUser, isPersistent: false);

				TempData["success"] = "Đăng ký và đăng nhập Google thành công!";
				return RedirectToAction("Index", "Home");
			}
			else
			{
				// User đã có → đăng nhập luôn
				await _signInManager.SignInAsync(existingUser, isPersistent: false);
				return RedirectToAction("Index", "Home");
			}
		}

	}
}

