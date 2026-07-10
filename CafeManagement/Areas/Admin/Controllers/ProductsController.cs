using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync());
        }

        private async Task LoadCategoriesAsync(int? selected = null)
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }
            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm món.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            await LoadCategoriesAsync(product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model.CategoryId);
                return View(model);
            }
            _context.Products.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật món.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xoá món.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xoá món đã có trong hoá đơn. Hãy ẩn (bỏ chọn 'Còn bán') thay vì xoá.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
