using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UnisexClothes.Models;
using System.Linq;

namespace UnisexClothes.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UniStyleDbContext _context;

        public HomeController(ILogger<HomeController> logger, UniStyleDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories
                .Where(c => c.IsActive ?? true)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            return View(categories);
        }

        // API: Get featured products for homepage
        [HttpGet]
        public IActionResult GetFeaturedProducts(int take = 8, int skip = 0)
        {
            try
            {
                var categoriesMap = _context.Categories.ToDictionary(c => c.CategoryId, c => c.CategoryName);

                var featuredProducts = _context.Products
                    .Where(p => p.IsActive ?? true)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                var products = featuredProducts
                    .Select(p => new
                    {
                        id = p.ProductId,
                        name = p.ProductName,
                        image = p.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                        price = p.Price,
                        discount = p.DiscountPercent,
                        salePrice = p.Price * (1 - (decimal)(p.DiscountPercent ?? 0) / 100),
                        rating = p.Rating,
                        categoryId = p.CategoryId,
                        category = categoriesMap.TryGetValue(p.CategoryId, out var catName) ? catName : "Danh mục khác",
                        categorySlug = $"cat-{p.CategoryId}"
                    })
                    .ToList();

                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
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