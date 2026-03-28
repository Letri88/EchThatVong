using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;
using ShoesShop.Repository;

namespace ShoesShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly DataContext _dataContext;

        public ChatController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        // Danh sách cuộc trò chuyện (theo User)
        public async Task<IActionResult> Index()
        {
            var conversations = await _dataContext.ChatMessages
                .GroupBy(m => m.ConversationId)
                .Select(g => new
                {
                    ConversationId = g.Key,
                    UserName = g.Where(m => !m.IsFromAdmin).Select(m => m.UserName).FirstOrDefault() ?? "Khách",
                    LastMessageTime = g.Max(m => m.CreatedAt),
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).Select(m => m.Message).FirstOrDefault()
                })
                .OrderByDescending(x => x.LastMessageTime)
                .ToListAsync();

            return View(conversations);
        }

        // Chi tiết 1 cuộc trò chuyện
        public async Task<IActionResult> Thread(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            // Chỉ load 7 tin nhắn gần nhất (tăng hiệu năng + UX giống Messenger)
            var messages = await _dataContext.ChatMessages
                .Where(m => m.ConversationId == id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(7)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.ConversationId = id;
            ViewBag.UserName = messages.FirstOrDefault()?.UserName ?? "Khách";

            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetThreadJson(string id, int? lastId)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var query = _dataContext.ChatMessages
                .Where(m => m.ConversationId == id);

            if (lastId.HasValue)
            {
                query = query.Where(m => m.Id > lastId.Value);
            }

            var messages = await query
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

            return Json(new { success = true, messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetThreadOlderJson(string id, int beforeId, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(id) || beforeId <= 0)
            {
                return BadRequest();
            }

            pageSize = Math.Clamp(pageSize, 1, 50);

            var older = await _dataContext.ChatMessages
                .Where(m => m.ConversationId == id && m.Id < beforeId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(pageSize)
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

            return Json(new { success = true, messages = older });
        }

        [HttpPost]
        public async Task<IActionResult> SendReply(string conversationId, string message, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(conversationId) && (image == null || image.Length == 0))
            {
                return RedirectToAction("Thread", new { id = conversationId });
            }

            string? imagePath = null;
            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine("wwwroot", "uploads", "chat");
                Directory.CreateDirectory(uploads);
                var fileName = $"admin_{conversationId}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(image.FileName)}";
                var fullPath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                imagePath = $"/uploads/chat/{fileName}";
            }

            if (string.IsNullOrWhiteSpace(message) && imagePath == null)
            {
                return RedirectToAction("Thread", new { id = conversationId });
            }

            var msg = new ChatMessageModel
            {
                ConversationId = conversationId,
                UserId = null,
                UserName = "Shop",
                Message = message ?? string.Empty,
                IsFromAdmin = true,
                CreatedAt = DateTime.Now,
                ImagePath = imagePath
            };

            _dataContext.ChatMessages.Add(msg);
            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Thread", new { id = conversationId });
        }

        [HttpPost]
        public async Task<IActionResult> SendReplyJson(string conversationId, string message, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || (string.IsNullOrWhiteSpace(message) && (image == null || image.Length == 0)))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung hoặc chọn hình." });
            }

            string? imagePath = null;
            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine("wwwroot", "uploads", "chat");
                Directory.CreateDirectory(uploads);
                var fileName = $"admin_{conversationId}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(image.FileName)}";
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
                UserId = null,
                UserName = "Shop",
                Message = message ?? string.Empty,
                IsFromAdmin = true,
                CreatedAt = DateTime.Now,
                ImagePath = imagePath
            };

            _dataContext.ChatMessages.Add(msg);
            await _dataContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = new
                {
                    msg.Id,
                    msg.Message,
                    msg.IsFromAdmin,
                    msg.ImagePath,
                    CreatedAt = msg.CreatedAt.ToString("HH:mm dd/MM")
                }
            });
        }
    }
}

