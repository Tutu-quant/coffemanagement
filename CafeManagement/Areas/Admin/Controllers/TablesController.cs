using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class TablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TablesController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            return View(await _context.RestaurantTables.OrderBy(t => t.TableName).ToListAsync());
        }

        public IActionResult Create() => View(new RestaurantTable());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RestaurantTable model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.RestaurantTables.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm bàn.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.RestaurantTables.FindAsync(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RestaurantTable model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.RestaurantTables.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật bàn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.RestaurantTables.FindAsync(id);
            if (table == null) return NotFound();
            _context.RestaurantTables.Remove(table);
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xoá bàn.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xoá bàn đã có lịch sử đặt bàn/hoá đơn. Hãy chuyển trạng thái 'Bảo trì' thay vì xoá.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
