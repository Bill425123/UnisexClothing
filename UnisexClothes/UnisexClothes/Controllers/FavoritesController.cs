using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;
using System.Text.Json;

namespace UnisexClothes.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly UniStyleDbContext _context;
        private const string SessionFavoritesKey = "SessionFavorites";

        public FavoritesController(UniStyleDbContext context)
        {
            _context = context;
        }

        // Trang Favorites - Sản phẩm yêu thích
        public IActionResult Index()
        {
            return View();
        }

        #region Helper - User & Session Favorites
        private int? GetUserId()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return null;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            return user?.UserId;
        }

        private List<FavoriteSessionItem> GetSessionFavorites()
        {
            var sessionData = HttpContext.Session.GetString(SessionFavoritesKey);
            if (string.IsNullOrEmpty(sessionData))
                return new List<FavoriteSessionItem>();

            return JsonSerializer.Deserialize<List<FavoriteSessionItem>>(sessionData)!;
        }

        private void SaveSessionFavorites(List<FavoriteSessionItem> favorites)
        {
            var json = JsonSerializer.Serialize(favorites);
            HttpContext.Session.SetString(SessionFavoritesKey, json);
        }

        public class FavoriteSessionItem
        {
            public int ProductId { get; set; }
            public DateTime AddedAt { get; set; }
        }
        #endregion

        // API: Thêm/Xóa sản phẩm vào danh sách yêu thích (Toggle)
        [HttpPost]
        public IActionResult ToggleFavorite([FromBody] ToggleFavoriteRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

                int productId = request.ProductId;

                // Kiểm tra sản phẩm có tồn tại không
                var product = _context.Products
                    .FirstOrDefault(p => p.ProductId == productId && (p.IsActive ?? true));

                if (product == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

                var userId = GetUserId();
                bool isAdded = false;

                Console.WriteLine($"[DEBUG] ToggleFavorite - ProductId: {productId}, UserId: {userId}");

                if (userId != null)
                {
                    // Đăng nhập → toggle trong DB
                    var existingFavorite = _context.Favorites
                        .FirstOrDefault(f => f.UserId == userId.Value && f.ProductId == productId);

                    if (existingFavorite != null)
                    {
                        // Đã có → Xóa
                        Console.WriteLine($"[DEBUG] Removing favorite from DB");
                        _context.Favorites.Remove(existingFavorite);
                        isAdded = false;
                    }
                    else
                    {
                        // Chưa có → Thêm
                        Console.WriteLine($"[DEBUG] Adding favorite to DB");
                        _context.Favorites.Add(new Favorite
                        {
                            UserId = userId.Value,
                            ProductId = productId,
                            AddedAt = DateTime.Now
                        });
                        isAdded = true;
                    }
                    _context.SaveChanges();
                    Console.WriteLine($"[DEBUG] DB changes saved. IsAdded: {isAdded}");
                }
                else
                {
                    // Chưa đăng nhập → toggle trong session
                    var sessionFavorites = GetSessionFavorites();
                    Console.WriteLine($"[DEBUG] Current session favorites count: {sessionFavorites.Count}");
                    
                    var item = sessionFavorites.FirstOrDefault(f => f.ProductId == productId);
                    
                    if (item != null)
                    {
                        // Đã có → Xóa
                        Console.WriteLine($"[DEBUG] Removing favorite from session");
                        sessionFavorites.Remove(item);
                        isAdded = false;
                    }
                    else
                    {
                        // Chưa có → Thêm
                        Console.WriteLine($"[DEBUG] Adding favorite to session");
                        sessionFavorites.Add(new FavoriteSessionItem
                        {
                            ProductId = productId,
                            AddedAt = DateTime.Now
                        });
                        isAdded = true;
                    }
                    SaveSessionFavorites(sessionFavorites);
                    Console.WriteLine($"[DEBUG] Session saved. New count: {sessionFavorites.Count}, IsAdded: {isAdded}");
                }

                return Json(new { 
                    success = true, 
                    isAdded = isAdded,
                    message = isAdded ? "Đã thêm vào danh sách yêu thích!" : "Đã xóa khỏi danh sách yêu thích!" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ToggleFavorite: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Lấy danh sách sản phẩm yêu thích
        [HttpGet]
        public IActionResult GetFavorites()
        {
            try
            {
                var userId = GetUserId();
                var favoriteItems = new List<object>();

                Console.WriteLine($"[DEBUG] GetFavorites - UserId: {userId}");

                if (userId != null)
                {
                    var dbFavorites = _context.Favorites
                        .Where(f => f.UserId == userId.Value)
                        .Include(f => f.Product)
                        .ToList();

                    Console.WriteLine($"[DEBUG] Found {dbFavorites.Count} favorites in DB");

                    favoriteItems = dbFavorites
                        .OrderByDescending(f => f.AddedAt)
                        .Select(f => new
                        {
                            favoriteId = f.FavoriteId,
                            productId = f.ProductId,
                            productName = f.Product.ProductName,
                            productImage = f.Product.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                            price = f.Product.Price,
                            discount = f.Product.DiscountPercent ?? 0,
                            salePrice = f.Product.Price * (1 - (decimal)(f.Product.DiscountPercent ?? 0) / 100),
                            stock = f.Product.StockQuantity ?? 0,
                            addedAt = f.AddedAt,
                            categoryId = f.Product.CategoryId,
                            description = f.Product.Description
                        })
                        .ToList<object>();
                }
                else
                {
                    // Chưa đăng nhập → lấy từ session
                    var sessionFavorites = GetSessionFavorites();
                    Console.WriteLine($"[DEBUG] Found {sessionFavorites.Count} favorites in session");

                    favoriteItems = sessionFavorites.Select(f =>
                    {
                        var product = _context.Products.FirstOrDefault(p => p.ProductId == f.ProductId && (p.IsActive ?? true));
                        if (product == null)
                        {
                            Console.WriteLine($"[DEBUG] Product {f.ProductId} not found or inactive");
                            return null;
                        }

                        return new
                        {
                            favoriteId = f.ProductId, // Dùng productId làm favoriteId cho session
                            productId = product.ProductId,
                            productName = product.ProductName,
                            productImage = product.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                            price = product.Price,
                            discount = product.DiscountPercent ?? 0,
                            salePrice = product.Price * (1 - (decimal)(product.DiscountPercent ?? 0) / 100),
                            stock = product.StockQuantity ?? 0,
                            addedAt = f.AddedAt,
                            categoryId = product.CategoryId,
                            description = product.Description
                        };
                    }).Where(x => x != null).ToList<object>();
                }

                Console.WriteLine($"[DEBUG] Returning {favoriteItems.Count} favorites");
                return Json(new { success = true, data = favoriteItems });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetFavorites: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // TEST API - Debug session
        [HttpGet]
        public IActionResult TestSession()
        {
            var userId = GetUserId();
            var sessionFavorites = GetSessionFavorites();
            
            return Json(new
            {
                userId = userId,
                sessionFavoritesCount = sessionFavorites.Count,
                sessionFavorites = sessionFavorites,
                userEmail = HttpContext.Session.GetString("UserEmail")
            });
        }

        // API: Lấy số lượng sản phẩm yêu thích (dùng cho header)
        [HttpGet]
        public IActionResult GetFavoritesCount()
        {
            try
            {
                var userId = GetUserId();
                int count = 0;

                if (userId != null)
                    count = _context.Favorites.Where(f => f.UserId == userId.Value).Count();
                else
                    count = GetSessionFavorites().Count();

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}", count = 0 });
            }
        }

        // API: Kiểm tra sản phẩm có trong danh sách yêu thích không
        [HttpGet]
        public IActionResult CheckFavorite(int productId)
        {
            try
            {
                var userId = GetUserId();
                bool isFavorite = false;

                if (userId != null)
                {
                    isFavorite = _context.Favorites
                        .Any(f => f.UserId == userId.Value && f.ProductId == productId);
                }
                else
                {
                    var sessionFavorites = GetSessionFavorites();
                    isFavorite = sessionFavorites.Any(f => f.ProductId == productId);
                }

                return Json(new { success = true, isFavorite = isFavorite });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}", isFavorite = false });
            }
        }

        // API: Xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost]
        public IActionResult RemoveFromFavorites([FromBody] RemoveFromFavoritesRequest request)
        {
            try
            {
                var userId = GetUserId();

                if (userId != null)
                {
                    var favoriteItem = _context.Favorites
                        .FirstOrDefault(f => f.FavoriteId == request.FavoriteId && f.UserId == userId.Value);
                    
                    if (favoriteItem != null)
                    {
                        _context.Favorites.Remove(favoriteItem);
                        _context.SaveChanges();
                        return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
                    }
                }
                else
                {
                    // Xóa từ session favorites
                    var sessionFavorites = GetSessionFavorites();
                    var item = sessionFavorites.FirstOrDefault(f => f.ProductId == request.FavoriteId);
                    
                    if (item != null)
                    {
                        sessionFavorites.Remove(item);
                        SaveSessionFavorites(sessionFavorites);
                        return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // Request models
    public class ToggleFavoriteRequest
    {
        public int ProductId { get; set; }
    }

    public class RemoveFromFavoritesRequest
    {
        public int FavoriteId { get; set; }
    }
}
