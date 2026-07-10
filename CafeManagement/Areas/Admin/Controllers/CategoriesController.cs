using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoriesController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync());
        }

        public IActionResult Create() => View(new Category());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm danh mục.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Categories.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();
            if (cat.Products.Any())
            {
                TempData["Error"] = "Không thể xoá danh mục còn món ăn bên trong.";
                return RedirectToAction(nameof(Index));
            }
            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xoá danh mục.";
            return RedirectToAction(nameof(Index));
        }
    }
}
