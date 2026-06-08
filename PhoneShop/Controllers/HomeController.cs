using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneShop.DB;
using PhoneShop.Models;
using System.Diagnostics;

namespace PhoneShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly PhoneShopDbContext _context;

        public HomeController(PhoneShopDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            var products = await _context.Products.ToListAsync();
            var featuredProducts = products.Where(p => p.Featured == true).ToList();

            ViewBag.Categories = categories;
            ViewBag.Products = products;
            ViewBag.FeaturedProducts = featuredProducts;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
