using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoesShop.Models;
using ShoesShop.Repository;

namespace ShoesShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Publisher,Author")]
    public class BotQuestionController : Controller
    {
        private readonly DataContext _dataContext;

        public BotQuestionController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IActionResult> Index()
        {
            // Eager load parent question
            var questions = await _dataContext.BotQuestions
                .Include(q => q.ParentQuestion)
                .OrderBy(q => q.OrderIndex)
                .ThenBy(q => q.Id)
                .ToListAsync();
            return View(questions);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ParentList = new SelectList(await _dataContext.BotQuestions.ToListAsync(), "Id", "UserMessage");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BotQuestionModel botQuestion)
        {
            if (ModelState.IsValid)
            {
                _dataContext.Add(botQuestion);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm câu hỏi mới thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentList = new SelectList(await _dataContext.BotQuestions.ToListAsync(), "Id", "UserMessage", botQuestion.ParentId);
            return View(botQuestion);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var botQuestion = await _dataContext.BotQuestions.FindAsync(id);
            if (botQuestion == null)
            {
                return NotFound();
            }
            // Không cho chọn chính nó làm cha
            var parents = await _dataContext.BotQuestions.Where(q => q.Id != id).ToListAsync();
            ViewBag.ParentList = new SelectList(parents, "Id", "UserMessage", botQuestion.ParentId);
            return View(botQuestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BotQuestionModel botQuestion)
        {
            if (id != botQuestion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dataContext.Update(botQuestion);
                    await _dataContext.SaveChangesAsync();
                    TempData["success"] = "Cập nhật câu hỏi thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BotQuestionExists(botQuestion.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            var parents = await _dataContext.BotQuestions.Where(q => q.Id != id).ToListAsync();
            ViewBag.ParentList = new SelectList(parents, "Id", "UserMessage", botQuestion.ParentId);
            return View(botQuestion);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var botQuestion = await _dataContext.BotQuestions.FindAsync(id);
            if (botQuestion == null)
            {
                return NotFound();
            }

            // check if it has children
            bool hasChildren = await _dataContext.BotQuestions.AnyAsync(q => q.ParentId == id);
            if (hasChildren)
            {
                TempData["error"] = "Không thể xóa câu hỏi này vì đang có câu hỏi con phụ thuộc.";
                return RedirectToAction(nameof(Index));
            }

            _dataContext.BotQuestions.Remove(botQuestion);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Đã xóa câu hỏi thành công";
            return RedirectToAction(nameof(Index));
        }

        private bool BotQuestionExists(int id)
        {
            return _dataContext.BotQuestions.Any(e => e.Id == id);
        }
    }
}
