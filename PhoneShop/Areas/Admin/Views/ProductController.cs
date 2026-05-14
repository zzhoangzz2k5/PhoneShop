using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneShop.DB;
using PhoneShop.Models;

namespace PhoneShop.Areas.Admin.Views
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        PhoneShopDbContext _context;
        public ProductController(PhoneShopDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.Category).ToArrayAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var cates = await _context.Categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToArrayAsync();
            ViewBag.CategoryId = cates;
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> Create(Product p)
        {
            if (ModelState.IsValid)
            {
                return View(p);
            }
            await _context.Products.AddAsync(p);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        }
    }

