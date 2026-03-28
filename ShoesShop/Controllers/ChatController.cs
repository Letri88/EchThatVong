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

