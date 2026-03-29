using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;
using ShoesShop.Models.ViewModels;
using ShoesShop.Services;
using ShoesShop.Repository;

namespace ShoesShop.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatController : Controller
    {
        private readonly GeminiChatService _geminiChatService;
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;

        public ChatController(GeminiChatService geminiChatService,
                              DataContext dataContext,
                              UserManager<AppUserModel> userManager)
        {
            _geminiChatService = geminiChatService;
            _dataContext = dataContext;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Suggest([FromBody] ChatRequestViewModel request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung câu hỏi." });
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName ?? "Khách vãng lai";
            var conversationId = userId ?? HttpContext.Connection.Id;

            var promptBuilder = new System.Text.StringBuilder();

            promptBuilder.AppendLine("Thông tin khách hàng:");
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                promptBuilder.AppendLine($"- Tên: {request.FullName}");
            }

            if (request.BirthDate.HasValue)
            {
                promptBuilder.AppendLine($"- Ngày sinh: {request.BirthDate:dd/MM/yyyy}");
            }

            if (!string.IsNullOrWhiteSpace(request.Gender))
            {
                promptBuilder.AppendLine($"- Giới tính: {request.Gender}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Yêu cầu của khách:");
            promptBuilder.AppendLine(request.Message);

            var reply = await _geminiChatService.GetBraceletSuggestionAsync(promptBuilder.ToString(), cancellationToken);

            // Lưu lịch sử chat bot (user message + bot reply)
            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId,
                UserId = userId,
                UserName = userName,
                Role = "user",
                Message = request.Message ?? string.Empty,
                CreatedAt = DateTime.Now
            });

            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId,
                UserId = userId,
                UserName = userName,
                Role = "bot",
                Message = reply ?? string.Empty,
                CreatedAt = DateTime.Now
            });

            await _dataContext.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = reply });
        }

        [HttpGet]
        public async Task<IActionResult> GetBotConversation(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var conversationId = userId ?? HttpContext.Connection.Id;

            var messages = await _dataContext.BotChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.Role,
                    m.Message,
                    CreatedAt = m.CreatedAt.ToString("HH:mm dd/MM")
                })
                .ToListAsync(cancellationToken);

            return Ok(new { success = true, messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetBotQuestions(int? parentId, CancellationToken cancellationToken)
        {
            var questions = await _dataContext.BotQuestions
                .Where(q => q.ParentId == parentId && q.Status == 1)
                .OrderBy(q => q.OrderIndex)
                .ThenBy(q => q.Id)
                .Select(q => new
                {
                    q.Id,
                    q.UserMessage
                })
                .ToListAsync(cancellationToken);

            return Ok(new { success = true, questions });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessBotQuestion([FromBody] int questionId, CancellationToken cancellationToken)
        {
            var question = await _dataContext.BotQuestions.FindAsync(new object[] { questionId }, cancellationToken);
            if (question == null || question.Status == 0)
            {
                return NotFound(new { success = false, message = "Câu hỏi không tồn tại." });
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName ?? "Khách vãng lai";
            var conversationId = userId ?? HttpContext.Connection.Id;

            // Lưu message
            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId, UserId = userId, UserName = userName, Role = "user",
                Message = question.UserMessage ?? string.Empty, CreatedAt = DateTime.Now
            });

            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId, UserId = userId, UserName = userName, Role = "bot",
                Message = question.BotReply ?? string.Empty, CreatedAt = DateTime.Now
            });
            await _dataContext.SaveChangesAsync(cancellationToken);

            // Tìm câu hỏi con
            var nextQuestions = await _dataContext.BotQuestions
                .Where(q => q.ParentId == question.Id && q.Status == 1)
                .OrderBy(q => q.OrderIndex).Select(q => new { q.Id, q.UserMessage })
                .ToListAsync(cancellationToken);

            // Lấy danh mục nếu có
            object? categories = null;
            if (question.ShowCategoryOptions)
            {
                categories = await _dataContext.Categories.Where(c => c.Status == 1).Select(c => new { c.Id, c.Name }).ToListAsync(cancellationToken);
            }

            return Ok(new
            {
                success = true,
                message = question.BotReply,
                nextQuestions,
                categories
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCategoryOption([FromBody] int categoryId, CancellationToken cancellationToken)
        {
            var category = await _dataContext.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
            if (category == null) return NotFound(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName ?? "Khách vãng lai";
            var conversationId = userId ?? HttpContext.Connection.Id;

            var userMsg = $"Xem danh mục: {category.Name}";
            
            var products = await _dataContext.Products
                .Where(p => p.CategoryId == categoryId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .Select(p => new { p.Id, p.Name, p.Image, p.Price })
                .ToListAsync(cancellationToken);

            // Tạo HTML trả lời có chứa ảnh
            var botReplyBuilder = new System.Text.StringBuilder();
            if (products.Count == 0)
            {
                botReplyBuilder.Append($"Hiện tại danh mục <b>{category.Name}</b> chưa có sản phẩm nào.");
            }
            else
            {
                botReplyBuilder.Append($"Tôi có vài gợi ý sản phẩm thuộc danh mục <b>{category.Name}</b> dành cho bạn:<br/><div class='ai-product-suggestions' style='display:flex; gap:10px; overflow-x:auto; margin-top:10px; padding-bottom:5px;'>");
                foreach(var p in products)
                {
                    var priceStr = p.Price.ToString("#,##0") + "đ";
                    botReplyBuilder.Append($"" +
                        $"<a href='/Product/Details/{p.Id}' target='_blank' style='flex: 0 0 auto; width:120px; border:1px solid #eee; border-radius:5px; padding:5px; text-decoration:none; color:#333; background:#fff; text-align:center;'>" +
                        $"<img src='/media/products/{p.Image}' style='width:100px; height:100px; object-fit:cover; border-radius:4px; display:block; margin:0 auto;'/>" +
                        $"<div style='font-size:12px; margin-top:5px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;'>{p.Name}</div>" +
                        $"<strong style='font-size:12px; display:block; color:#FE980F;'>{priceStr}</strong>" +
                        $"</a>");
                }
                botReplyBuilder.Append("</div>");
            }

            var botReply = botReplyBuilder.ToString();

            // Lưu message
            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId, UserId = userId, UserName = userName, Role = "user",
                Message = userMsg, CreatedAt = DateTime.Now
            });

            _dataContext.BotChatMessages.Add(new BotChatMessageModel
            {
                ConversationId = conversationId, UserId = userId, UserName = userName, Role = "bot",
                Message = botReply, CreatedAt = DateTime.Now
            });
            await _dataContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = botReply
            });
        }

        // =============== CHAT VỚI SHOP ===============

        [HttpPost]
        public async Task<IActionResult> SendToShop([FromForm] ShopChatRequestViewModel request, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(request.Message) && (image == null || image.Length == 0))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung hoặc chọn hình." });
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName ?? "Khách vãng lai";
            var conversationId = userId ?? HttpContext.Connection.Id;

            string? imagePath = null;
            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine("wwwroot", "uploads", "chat");
                Directory.CreateDirectory(uploads);
                var fileName = $"{conversationId}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(image.FileName)}";
                var fullPath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                imagePath = $"/uploads/chat/{fileName}";
            }

            var msg = new ChatMessageModel
            {
                ConversationId = conversationId,
                UserId = userId,
                UserName = userName,
                Message = request.Message ?? string.Empty,
                IsFromAdmin = false,
                CreatedAt = DateTime.Now,
                ImagePath = imagePath
            };

            _dataContext.ChatMessages.Add(msg);
            await _dataContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var conversationId = userId ?? HttpContext.Connection.Id;

            var messages = await _dataContext.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.Message,
                    m.IsFromAdmin,
                    m.ImagePath,
                    CreatedAt = m.CreatedAt.ToString("HH:mm dd/MM")
                })
                .ToListAsync();

            return Ok(new { success = true, messages });
        }
    }
}

