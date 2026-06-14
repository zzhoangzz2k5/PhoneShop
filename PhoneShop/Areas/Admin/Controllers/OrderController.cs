using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.DB;
using PhoneShop.Models;

namespace PhoneShop.Areas.Admin.Controllers;

[Area("Admin")]
public class OrderController : Controller
{
    private readonly PhoneShopDbContext _db;

    public OrderController(PhoneShopDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        return View(await _db.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(item => item.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int status)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), status))
        {
            return BadRequest();
        }

        var order = await _db.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
