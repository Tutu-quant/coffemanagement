using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/products")]
public class ProductsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? search)
    {
        IQueryable<Models.Entities.Product> query = context.Products.AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted);
        if (categoryId.HasValue) query = query.Where(p => p.CategoryID == categoryId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.ProductName.Contains(search.Trim()));
        return Ok(await query.OrderBy(p => p.ProductName).Select(p => new { p.ProductID, p.ProductName, p.Price, p.Quantity, p.Description, p.ImageUrl, p.CategoryID, Category = p.Category!.CategoryName }).ToListAsync());
    }
}
