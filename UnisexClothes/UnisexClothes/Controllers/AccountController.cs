using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;
using System.Text.Json;

namespace UnisexClothes.Controllers
{
    public class AccountController : Controller
    {
        private readonly UniStyleDbContext _context;

        public AccountController(UniStyleDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] string name, [FromForm] string email, [FromForm] string phone, [FromForm] string password)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });
                }

                // Validate email format
                if (!email.Contains("@") || !email.Contains("."))
                {
                    return Json(new { success = false, message = "Email không hợp lệ!" });
                }

                // Validate password length
                if (password.Length < 8)
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự!" });
                }

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã được đăng ký!" });
                }

                // Create new user
                var newUser = new User
                {
                    FullName = name,
                    Email = email,
                    PhoneNumber = phone,
                    Password = password, // In production, hash this password!
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    SpinNumber = 3 // Default 3 free spins for new users
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đăng ký thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi đăng ký! Vui lòng thử lại.",
                    error = ex.Message
                });
            }
        }

        public async Task<IActionResult> MyOrders()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("MyOrders", "Account") ?? "/Account/MyOrders");
                return RedirectToAction("Login");
            }

            // Get user's orders with details
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("OrderDetail", "Account", new { id }) ?? $"/Account/OrderDetail/{id}");
                return RedirectToAction("Login");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Login([FromBody] User model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });

                // Kiểm tra database
                var user = _context.Users
                            .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user == null)
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });

                // Lưu session (QUAN TRỌNG: Phải có UserId)
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserLoggedIn", "true");
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");
                HttpContext.Session.SetString("UserName", user.FullName ?? "User");

                // Migrate session cart to database
                MigrateSessionCartToDatabase(user.UserId);

                // Kiểm tra ReturnUrl để redirect về trang trước đó
                var returnUrl = HttpContext.Session.GetString("ReturnUrl");
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    HttpContext.Session.Remove("ReturnUrl");
                    return Json(new { 
                        success = true, 
                        message = "Đăng nhập thành công!",
                        redirectUrl = returnUrl
                    });
                }

                return Json(new { success = true, message = "Đăng nhập thành công!" });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Json(new { 
                    success = false, 
                    message = $"Lỗi đăng nhập: {ex.Message}",
                    error = ex.ToString() // Chỉ để debug, xóa đi khi production
                });
            }
        }

        // Migrate session cart to database when user logs in
        private void MigrateSessionCartToDatabase(int userId)
        {
            try
            {
                const string SessionCartKey = "SessionCart";
                var sessionData = HttpContext.Session.GetString(SessionCartKey);
                
                if (string.IsNullOrEmpty(sessionData))
                    return; // No session cart to migrate

                var sessionCart = JsonSerializer.Deserialize<List<CartSessionItem>>(sessionData);
                if (sessionCart == null || sessionCart.Count == 0)
                    return;

                foreach (var item in sessionCart)
                {
                    // Check if product exists and is active
                    var product = _context.Products
                        .FirstOrDefault(p => p.ProductId == item.ProductId && (p.IsActive ?? true));
                    
                    if (product == null)
                        continue; // Skip invalid products

                    // Check if item already exists in database cart
                    var existingCartItem = _context.Carts
                        .FirstOrDefault(c => c.UserId == userId 
                            && c.ProductId == item.ProductId 
                            && c.VariantId == item.VariantId);

                    if (existingCartItem != null)
                    {
                        // Merge: add session quantity to existing quantity
                        // Check stock first
                        int availableStock = product.StockQuantity ?? 0;
                        if (item.VariantId.HasValue)
                        {
                            var variant = _context.ProductVariants
                                .FirstOrDefault(v => v.VariantId == item.VariantId.Value && v.ProductId == item.ProductId);
                            if (variant != null)
                                availableStock = variant.StockQuantity ?? 0;
                        }

                        var newQuantity = existingCartItem.Quantity + item.Quantity;
                        existingCartItem.Quantity = Math.Min(newQuantity, availableStock);
                        existingCartItem.AddedAt = DateTime.Now;
                    }
                    else
                    {
                        // Add new item to database
                        _context.Carts.Add(new Cart
                        {
                            UserId = userId,
                            ProductId = item.ProductId,
                            VariantId = item.VariantId,
                            Quantity = item.Quantity,
                            AddedAt = DateTime.Now
                        });
                    }
                }

                _context.SaveChanges();

                // Clear session cart after migration
                HttpContext.Session.Remove(SessionCartKey);
            }
            catch (Exception ex)
            {
                // Log error but don't fail login
                Console.WriteLine($"Error migrating session cart: {ex.Message}");
            }
        }

        private class CartSessionItem
        {
            public int ProductId { get; set; }
            public int? VariantId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
