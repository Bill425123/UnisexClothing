using Microsoft.AspNetCore.Mvc;
using UnisexClothes.Models;
using System.Linq;

namespace UnisexClothes.Controllers
{
    /// <summary>
    /// Admin Panel Controller
    /// URL: /Admin/...
    /// </summary>
    public class AdminController : Controller
    {
        private readonly UniStyleDbContext _context;

        public AdminController(UniStyleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Admin Login Page
        /// URL: /Admin/Login
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Process Admin Login
        /// </summary>
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            try
            {
                // Tìm admin theo email (ưu tiên) hoặc FullName
                var admin = _context.Admins
                    .FirstOrDefault(a => (a.Email == username || a.FullName == username) && a.Password == password);

                if (admin != null)
                {
                    // Kiểm tra admin có đang active không
                    if (!admin.IsActive)
                    {
                        ViewBag.Error = "Tài khoản admin đã bị khóa!";
                        return View();
                    }

                    // Kiểm tra role - chấp nhận SuperAdmin và Admin
                    var role = admin.Role?.Trim() ?? "";
                    if (string.IsNullOrEmpty(role) || 
                        (role.ToLower() != "admin" && role.ToLower() != "superadmin"))
                    {
                        ViewBag.Error = "Bạn không có quyền truy cập!";
                        return View();
                    }

                    // Set session
                    HttpContext.Session.SetString("AdminLoggedIn", "true");
                    HttpContext.Session.SetString("AdminUsername", admin.FullName);
                    HttpContext.Session.SetString("AdminEmail", admin.Email);
                    HttpContext.Session.SetString("AdminRole", role);
                    HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                    
                    return RedirectToAction("Dashboard");
                }
                
                ViewBag.Error = "Email hoặc mật khẩu không đúng!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi đăng nhập: {ex.Message}";
                return View();
            }
        }

        /// <summary>
        /// Admin Logout
        /// URL: /Admin/Logout
        /// </summary>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Admin Dashboard
        /// URL: /Admin/Dashboard hoặc /Admin
        /// </summary>
        public IActionResult Dashboard()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            // Get statistics from database
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalRevenue = _context.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;

            return View();
        }

        /// <summary>
        /// Categories Management
        /// URL: /Admin/Categories
        /// </summary>
        public IActionResult Categories()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// Get All Categories API
        /// URL: /Admin/GetCategories
        /// </summary>
        [HttpGet]
        public IActionResult GetCategories()
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var categories = _context.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        id = c.CategoryId,
                        name = c.CategoryName,
                        image = c.CategoryImage,
                        description = c.Description,
                        displayOrder = c.DisplayOrder,
                        isActive = c.IsActive,
                        productCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                    })
                    .ToList();

                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Add/Edit Category - POST
        /// URL: /Admin/CategoryEdit
        /// </summary>
        [HttpPost]
        public IActionResult CategoryEdit(int categoryId, string categoryName, string? description, 
            int displayOrder, string? categoryImage, bool isActive)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                Category? category;

                if (categoryId > 0)
                {
                    // Update existing category
                    category = _context.Categories.Find(categoryId);
                    if (category == null)
                        return Json(new { success = false, message = "Không tìm thấy danh mục!" });
                }
                else
                {
                    // Create new category
                    category = new Category();
                    _context.Categories.Add(category);
                }

                // Update fields
                category.CategoryName = categoryName;
                category.Description = description;
                category.DisplayOrder = displayOrder;
                category.CategoryImage = categoryImage;
                category.IsActive = isActive;

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = categoryId > 0 ? "Đã cập nhật danh mục thành công!" : "Đã thêm danh mục mới thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete Category
        /// URL: /Admin/CategoryDelete/{id}
        /// </summary>
        [HttpPost]
        public IActionResult CategoryDelete(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var category = _context.Categories.Find(id);
                if (category == null)
                    return Json(new { success = false, message = "Không tìm thấy danh mục!" });

                // Check if category has products
                var productCount = _context.Products.Count(p => p.CategoryId == id);
                if (productCount > 0)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Không thể xóa danh mục này! Có {productCount} sản phẩm đang sử dụng danh mục này." 
                    });
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Đã xóa danh mục '{category.CategoryName}' thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Product Management
        /// URL: /Admin/Products
        /// </summary>
        public IActionResult Products()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// Get All Products API with Pagination and Filters
        /// URL: /Admin/GetProducts?page=1&pageSize=8&search=&category=&status=
        /// </summary>
        [HttpGet]
        public IActionResult GetProducts(int page = 1, int pageSize = 8, string? search = null, string? category = null, string? status = null)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var query = _context.Products.AsQueryable();

                // Filter by search keyword
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.ProductName.Contains(search));
                }

                // Filter by category
                if (!string.IsNullOrWhiteSpace(category) && int.TryParse(category, out int categoryId))
                {
                    query = query.Where(p => p.CategoryId == categoryId);
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        query = query.Where(p => p.IsActive == true || p.IsActive == null);
                    else if (status == "inactive")
                        query = query.Where(p => p.IsActive == false);
                }

                // Order by ProductId descending
                query = query.OrderByDescending(p => p.ProductId);
                
                // Get total count for pagination
                var totalCount = query.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                // Get products for current page
                var products = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        id = p.ProductId,
                        name = p.ProductName,
                        image = p.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                        price = p.Price,
                        discount = p.DiscountPercent,
                        stock = p.StockQuantity,
                        categoryId = p.CategoryId,
                        category = p.CategoryId == 1 ? "Áo" : p.CategoryId == 2 ? "Quần" : "Phụ kiện",
                        status = (p.IsActive ?? true) ? "active" : "inactive",
                        rating = p.Rating,
                        viewCount = p.ViewCount
                    })
                    .ToList();

                return Json(new { 
                    success = true, 
                    data = products,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasPrevious = page > 1,
                        hasNext = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Upload Image (for Products or Categories)
        /// URL: /Admin/UploadImage
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file, string? type = "product")
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Không có file được chọn!" });

                // Cho phép mọi loại file và dung lượng (không khuyến nghị cho production)
                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".bin";

                // Determine upload path based on type
                string uploadPath;
                string imageUrl;
                
                if (type == "category")
                {
                    uploadPath = Path.Combine("wwwroot", "images", "HinhDanhMucUnisex");
                    var fileName = $"category_{DateTime.Now.Ticks}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    // Create directory if not exists
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);
                    
                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    imageUrl = $"/images/HinhDanhMucUnisex/{fileName}";
                }
                else
                {
                    // Default: product image
                    uploadPath = Path.Combine("wwwroot", "images", "HinhSanPhamUnisex");
                    var fileName = $"product_{DateTime.Now.Ticks}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    // Create directory if not exists
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);
                    
                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    imageUrl = $"/images/HinhSanPhamUnisex/{fileName}";
                }

                return Json(new { success = true, url = imageUrl, message = "Upload thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi upload: {ex.Message}" });
            }
        }

        /// <summary>
        /// Add/Edit Product - GET
        /// URL: /Admin/ProductEdit/{id?}
        /// </summary>
        [HttpGet]
        public IActionResult ProductEdit(int? id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            Product? product = null;
            
            if (id.HasValue)
            {
                // Load existing product
                product = _context.Products.Find(id.Value);
                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("Products");
                }
            }

            // Load all active categories from database
            var categories = _context.Categories
                .Where(c => c.IsActive ?? true)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            ViewBag.ProductId = id ?? 0;
            ViewBag.IsEdit = id.HasValue;
            ViewBag.Product = product;
            ViewBag.Categories = categories;
            
            return View();
        }

        /// <summary>
        /// Add/Edit Product - POST
        /// URL: /Admin/ProductEdit
        /// </summary>
        [HttpPost]
        public IActionResult ProductEdit(int productId, string productName, int categoryId, 
            string? description, decimal price, int discountPercent, int stockQuantity, string? productImage)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                Product? product;
                
                if (productId > 0)
                {
                    // Update existing product
                    product = _context.Products.Find(productId);
                    if (product == null)
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
                }
                else
                {
                    // Create new product
                    product = new Product
                    {
                        CreatedAt = DateTime.Now
                    };
                    _context.Products.Add(product);
                }

                // Update fields
                product.ProductName = productName;
                product.CategoryId = categoryId;
                product.Description = description;
                product.Price = price;
                product.DiscountPercent = discountPercent;
                product.StockQuantity = stockQuantity;
                product.ProductImage = productImage;
                
                if (productId > 0)
                {
                    product.UpdatedAt = DateTime.Now;
                }

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = productId > 0 ? "Đã cập nhật sản phẩm thành công!" : "Đã thêm sản phẩm mới thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete Product
        /// URL: /Admin/ProductDelete/{id}
        /// </summary>
        [HttpPost]
        public IActionResult ProductDelete(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var product = _context.Products.Find(id);
                if (product == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

                _context.Products.Remove(product);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Đã xóa sản phẩm '{product.ProductName}' thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Order Management
        /// URL: /Admin/Orders
        /// </summary>
        public IActionResult Orders()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// Order Detail
        /// URL: /Admin/OrderDetail/{id}
        /// </summary>
        public IActionResult OrderDetail(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            ViewBag.OrderId = id;
            return View();
        }

        /// <summary>
        /// Update Order Status in Database
        /// URL: /Admin/UpdateOrderStatus
        /// </summary>
        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, string status)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var order = _context.Orders.Find(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

                // Update order status
                order.OrderStatus = CapitalizeFirstLetter(status);
                
                // Update delivered date if status is completed
                if (status.ToLower() == "completed" && !order.DeliveredDate.HasValue)
                {
                    order.DeliveredDate = DateTime.Now;
                }

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành '{GetStatusText(status)}'" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Capitalize first letter of string
        /// </summary>
        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }

        /// <summary>
        /// User Management
        /// URL: /Admin/Users
        /// </summary>
        public IActionResult Users()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// Fee Management
        /// URL: /Admin/Fees
        /// </summary>
        public IActionResult Fees()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// API: Get all fees
        /// </summary>
        [HttpGet]
        public IActionResult GetFees()
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var fees = _context.Fees
                    .OrderBy(f => f.FeedId)
                    .Select(f => new
                    {
                        id = f.FeedId,
                        name = f.Name,
                        value = f.Value,
                        description = f.Description,
                        threshold = f.Threshold
                    })
                    .ToList();

                return Json(new { success = true, data = fees });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: Create or Update Fee
        /// </summary>
        [HttpPost]
        public IActionResult SaveFee([FromBody] Fee fee)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                if (fee.FeedId == 0)
                {
                    // Create new
                    _context.Fees.Add(fee);
                }
                else
                {
                    // Update existing
                    var existingFee = _context.Fees.Find(fee.FeedId);
                    if (existingFee == null)
                        return Json(new { success = false, message = "Không tìm thấy phí" });

                    existingFee.Name = fee.Name;
                    existingFee.Value = fee.Value;
                    existingFee.Description = fee.Description;
                    existingFee.Threshold = fee.Threshold;
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Lưu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: Delete Fee
        /// </summary>
        [HttpPost]
        public IActionResult DeleteFee(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var fee = _context.Fees.Find(id);
                if (fee == null)
                    return Json(new { success = false, message = "Không tìm thấy phí" });

                _context.Fees.Remove(fee);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Coupon Management
        /// URL: /Admin/Coupons
        /// </summary>
        public IActionResult Coupons()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        /// <summary>
        /// API: Get all coupons
        /// </summary>
        [HttpGet]
        public IActionResult GetCoupons()
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var coupons = _context.Coupons
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new
                    {
                        id = c.CouponId,
                        code = c.Code,
                        isUsed = c.IsUsed,
                        createdDate = c.CreatedDate,
                        expiryDate = c.ExpiryDate,
                        usedDate = c.UsedDate,
                        discountAmount = c.DiscountAmount
                    })
                    .ToList();

                return Json(new { success = true, data = coupons });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: Create or Update Coupon
        /// </summary>
        [HttpPost]
        public IActionResult SaveCoupon([FromBody] Coupon coupon)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                if (coupon.CouponId == 0)
                {
                    // Create new
                    coupon.CreatedDate = DateTime.Now;
                    coupon.IsUsed = false;
                    _context.Coupons.Add(coupon);
                }
                else
                {
                    // Update existing
                    var existingCoupon = _context.Coupons.Find(coupon.CouponId);
                    if (existingCoupon == null)
                        return Json(new { success = false, message = "Không tìm thấy coupon" });

                    existingCoupon.Code = coupon.Code;
                    existingCoupon.ExpiryDate = coupon.ExpiryDate;
                    existingCoupon.DiscountAmount = coupon.DiscountAmount;
                    existingCoupon.IsUsed = coupon.IsUsed;
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Lưu coupon thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: Delete Coupon
        /// </summary>
        [HttpPost]
        public IActionResult DeleteCoupon(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var coupon = _context.Coupons.Find(id);
                if (coupon == null)
                    return Json(new { success = false, message = "Không tìm thấy coupon" });

                if (coupon.IsUsed == true)
                    return Json(new { success = false, message = "Không thể xóa coupon đã sử dụng" });

                _context.Coupons.Remove(coupon);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa coupon thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Check if admin is logged in
        /// </summary>
        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("AdminLoggedIn") == "true";
        }

        /// <summary>
        /// Check if current admin is SuperAdmin
        /// </summary>
        private bool IsSuperAdmin()
        {
            var role = HttpContext.Session.GetString("AdminRole");
            return !string.IsNullOrEmpty(role) && role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get current admin role
        /// </summary>
        private string? GetCurrentAdminRole()
        {
            return HttpContext.Session.GetString("AdminRole");
        }

        /// <summary>
        /// API: Get Dashboard Stats from Database
        /// </summary>
        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false });

            try
            {
                var totalProducts = _context.Products.Count();
                var totalOrders = _context.Orders.Count();
                var totalUsers = _context.Users.Count();
                var totalRevenue = _context.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;
                
                var today = DateTime.Today;
                var todayOrders = _context.Orders.Count(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == today);
                var pendingOrders = _context.Orders.Count(o => o.OrderStatus == "Pending");

                // Get recent orders data first
                var recentOrdersData = _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(3)
                    .Select(o => new
                    {
                        id = o.OrderId,
                        customer = o.FullName,
                        total = o.TotalAmount,
                        orderStatus = o.OrderStatus,
                        date = o.OrderDate
                    })
                    .ToList();

                // Transform with status text
                var recentOrders = recentOrdersData.Select(o => new
                {
                    id = o.id,
                    customer = o.customer,
                    total = o.total,
                    status = GetStatusText(o.orderStatus),
                    date = o.date.HasValue ? o.date.Value.ToString("dd/MM/yyyy") : ""
                })
                .ToList();

            return Json(new
            {
                success = true,
                    totalProducts = totalProducts,
                    totalOrders = totalOrders,
                    totalUsers = totalUsers,
                    totalRevenue = totalRevenue,
                    todayOrders = todayOrders,
                    pendingOrders = pendingOrders,
                    recentOrders = recentOrders
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Get Orders from Database
        /// URL: /Admin/GetOrders
        /// </summary>
        [HttpGet]
        public IActionResult GetOrders()
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Get orders from database first
                var ordersData = _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        id = o.OrderId,
                        customer = o.FullName,
                        phone = o.PhoneNumber,
                        orderId = o.OrderId,
                        total = o.TotalAmount,
                        payment = o.PaymentMethod,
                        orderStatus = o.OrderStatus,
                        date = o.OrderDate
                    })
                    .ToList();

                // Transform data with item count and status conversion
                var orders = ordersData.Select(o => new
                {
                    id = o.id,
                    customer = o.customer,
                    phone = o.phone,
                    items = _context.OrderDetails.Count(od => od.OrderId == o.orderId),
                    total = o.total,
                    payment = o.payment,
                    status = ConvertOrderStatus(o.orderStatus),
                    date = o.date.HasValue ? o.date.Value.ToString("dd/MM/yyyy HH:mm") : ""
                })
                .ToList();

            return Json(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Convert OrderStatus to lowercase for frontend
        /// </summary>
        private string ConvertOrderStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return "pending";

            return status.ToLower() switch
            {
                "pending" => "pending",
                "processing" => "processing",
                "shipping" => "shipping",
                "completed" => "completed",
                "cancelled" => "cancelled",
                _ => "pending"
            };
        }

        /// <summary>
        /// API: Get Users
        /// URL: /Admin/GetUsers
        /// </summary>
        [HttpGet]
        public IActionResult GetUsers(string? search, string? role, string? status)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var query = _context.Users.AsQueryable();

                // Filter by search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.FullName.Contains(search) || 
                        u.Email.Contains(search) || 
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                    );
                }

                // Users are customers only, so filter by role is not needed
                // Filter by status is not applicable for Users (they are always active)

                var users = query
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        id = u.UserId,
                        name = u.FullName,
                        email = u.Email,
                        phone = u.PhoneNumber ?? "",
                        orders = 0, // TODO: Count orders from Orders table
                        status = "active", // Users are always active
                        role = "customer", // Users are always customers
                        createdAt = u.CreatedAt.HasValue ? u.CreatedAt.Value.ToString("dd/MM/yyyy") : ""
                    })
                    .ToList();

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }


        /// <summary>
        /// API: Add/Edit User
        /// URL: /Admin/UserEdit
        /// </summary>
        [HttpPost]
        public IActionResult UserEdit(int userId, string fullName, string email, string? phoneNumber, string? password, string? address)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                User? user;
                if (userId > 0)
                {
                    user = _context.Users.Find(userId);
                    if (user == null)
                        return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }
                else
                {
                    // Check if email already exists
                    if (_context.Users.Any(u => u.Email == email))
                        return Json(new { success = false, message = "Email đã tồn tại!" });

                    if (string.IsNullOrEmpty(password))
                        return Json(new { success = false, message = "Mật khẩu không được để trống!" });

                    user = new User();
                    _context.Users.Add(user);
                    user.Password = password; // TODO: Hash password in production
                }

                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                user.Address = address;
                // Users are always customers, Role is not applicable
                // Users are always active, IsActive is not applicable

                if (!string.IsNullOrEmpty(password) && userId > 0)
                {
                    user.Password = password; // TODO: Hash password in production
                }

                if (userId > 0)
                {
                    user.UpdatedAt = DateTime.Now;
                }

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = userId > 0 ? "Đã cập nhật người dùng thành công!" : "Đã thêm người dùng mới thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Delete User
        /// URL: /Admin/UserDelete
        /// </summary>
        [HttpPost]
        public IActionResult UserDelete(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = _context.Users.Find(id);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                _context.Users.Remove(user);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Đã xóa người dùng '{user.FullName}' thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Lock/Unlock User
        /// URL: /Admin/UserLock
        /// </summary>
        [HttpPost]
        public IActionResult UserLock(int id, bool lockUser)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = _context.Users.Find(id);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                // Users don't have IsActive property, so we can't lock/unlock them
                // Instead, we can delete them if needed
                user.UpdatedAt = DateTime.Now;
                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = "Cập nhật thành công! (Lưu ý: Users không có tính năng khóa/mở khóa)" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Get Order Detail from Database
        /// URL: /Admin/GetOrderDetail/{id}
        /// </summary>
        [HttpGet]
        public IActionResult GetOrderDetail(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var order = _context.Orders.Find(id);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

                // Get order details
                var orderDetails = _context.OrderDetails
                    .Where(od => od.OrderId == id)
                    .Select(od => new
                    {
                        productId = od.ProductId,
                        name = od.ProductName,
                        image = od.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                        price = od.UnitPrice,
                        quantity = od.Quantity,
                        subtotal = od.TotalPrice,
                        color = od.Color,
                        size = od.Size
                    })
                    .ToList();

            var orderDetail = new
            {
                    id = order.OrderId,
                    customer = new 
                    { 
                        name = order.FullName, 
                        email = order.Email, 
                        phone = order.PhoneNumber 
                    },
                    shippingAddress = order.ShippingAddress,
                    orderDate = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    status = ConvertOrderStatus(order.OrderStatus),
                    paymentMethod = order.PaymentMethod,
                    items = orderDetails,
                    subtotal = order.SubTotal,
                    shippingFee = order.ShippingFee ?? 0,
                    discountAmount = order.DiscountAmount ?? 0,
                    total = order.TotalAmount,
                    notes = order.Notes ?? "",
                statusHistory = new[]
                {
                        new 
                        { 
                            status = ConvertOrderStatus(order.OrderStatus), 
                            statusText = GetStatusText(order.OrderStatus), 
                            date = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy HH:mm") : "" 
                        }
                }
            };

            return Json(new { success = true, data = orderDetail });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get status text in Vietnamese
        /// </summary>
        private string GetStatusText(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return "Chờ xác nhận";

            return status.ToLower() switch
            {
                "pending" => "Chờ xác nhận",
                "processing" => "Đang xử lý",
                "shipping" => "Đang giao",
                "completed" => "Hoàn thành",
                "cancelled" => "Đã hủy",
                _ => "Chờ xác nhận"
            };
        }

        /// <summary>
        /// API: Get Admins
        /// URL: /Admin/GetAdmins
        /// </summary>
        [HttpGet]
        public IActionResult GetAdmins(string? search, string? role, string? status)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var query = _context.Admins.AsQueryable();

                // Filter by search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a => 
                        a.FullName.Contains(search) || 
                        a.Email.Contains(search) || 
                        (a.PhoneNumber != null && a.PhoneNumber.Contains(search))
                    );
                }

                // Filter by role
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(a => a.Role == role);
                }

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    bool isActive = status == "active";
                    query = query.Where(a => a.IsActive == isActive);
                }

                var admins = query
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        id = a.AdminId,
                        name = a.FullName,
                        email = a.Email,
                        phone = a.PhoneNumber ?? "",
                        role = a.Role ?? "admin",
                        status = a.IsActive ? "active" : "inactive",
                        address = a.Address ?? "",
                        createdAt = a.CreatedAt.HasValue ? a.CreatedAt.Value.ToString("dd/MM/yyyy") : ""
                    })
                    .ToList();

                return Json(new { success = true, data = admins });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Add/Edit Admin
        /// URL: /Admin/AdminEdit
        /// </summary>
        [HttpPost]
        public IActionResult AdminEdit(int adminId, string fullName, string email, string? phoneNumber, string? password, string? address, string role, bool isActive)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                Admin? admin;
                if (adminId > 0)
                {
                    admin = _context.Admins.Find(adminId);
                    if (admin == null)
                        return Json(new { success = false, message = "Không tìm thấy admin!" });
                }
                else
                {
                    // Check if email already exists
                    if (_context.Admins.Any(a => a.Email == email))
                        return Json(new { success = false, message = "Email đã tồn tại!" });

                    if (string.IsNullOrEmpty(password))
                        return Json(new { success = false, message = "Mật khẩu không được để trống!" });

                    admin = new Admin();
                    _context.Admins.Add(admin);
                    admin.Password = password; // TODO: Hash password in production
                    admin.CreatedAt = DateTime.Now;
                }

                admin.FullName = fullName;
                admin.Email = email;
                admin.PhoneNumber = phoneNumber;
                admin.Address = address;
                admin.Role = string.IsNullOrEmpty(role) ? "admin" : role;
                admin.IsActive = isActive;

                if (!string.IsNullOrEmpty(password) && adminId > 0)
                {
                    admin.Password = password; // TODO: Hash password in production
                }

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = adminId > 0 ? "Đã cập nhật admin thành công!" : "Đã thêm admin mới thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Delete Admin
        /// URL: /Admin/AdminDelete
        /// </summary>
        [HttpPost]
        public IActionResult AdminDelete(int id)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var admin = _context.Admins.Find(id);
                if (admin == null)
                    return Json(new { success = false, message = "Không tìm thấy admin!" });

                // Không cho phép xóa chính mình
                var currentAdminId = HttpContext.Session.GetInt32("AdminId");
                if (currentAdminId.HasValue && currentAdminId.Value == id)
                {
                    return Json(new { success = false, message = "Không thể xóa chính tài khoản của bạn!" });
                }

                _context.Admins.Remove(admin);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Đã xóa admin '{admin.FullName}' thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Lock/Unlock Admin
        /// URL: /Admin/AdminLock
        /// </summary>
        [HttpPost]
        public IActionResult AdminLock(int id, bool lockAdmin)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var admin = _context.Admins.Find(id);
                if (admin == null)
                    return Json(new { success = false, message = "Không tìm thấy admin!" });

                // Không cho phép khóa chính mình
                var currentAdminId = HttpContext.Session.GetInt32("AdminId");
                if (currentAdminId.HasValue && currentAdminId.Value == id)
                {
                    return Json(new { success = false, message = "Không thể khóa chính tài khoản của bạn!" });
                }

                admin.IsActive = !lockAdmin;
                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = lockAdmin ? $"Đã khóa admin '{admin.FullName}' thành công!" : $"Đã mở khóa admin '{admin.FullName}' thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Seed Sample Admins - Tạo dữ liệu mẫu cho admin
        /// URL: /Admin/SeedAdmins
        /// </summary>
        [HttpGet]
        public IActionResult SeedAdmins()
        {
            try
            {
                var createdCount = 0;
                var skippedCount = 0;

                // Kiểm tra và thêm Admin đầu tiên
                if (!_context.Admins.Any(a => a.Email == "admin@unistyle.com"))
                {
                    var admin = new Admin
                    {
                        FullName = "Admin UniStyle",
                        Email = "admin@unistyle.com",
                        Password = "admin123",
                        PhoneNumber = "0123456789",
                        Address = "TP. Hồ Chí Minh",
                        Role = "admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Admins.Add(admin);
                    createdCount++;
                }
                else
                {
                    skippedCount++;
                }

                // Thêm Manager
                if (!_context.Admins.Any(a => a.Email == "manager@unistyle.com"))
                {
                    var manager = new Admin
                    {
                        FullName = "Quản lý Store",
                        Email = "manager@unistyle.com",
                        Password = "manager123",
                        PhoneNumber = "0123456788",
                        Address = "TP. Hồ Chí Minh",
                        Role = "admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-60)
                    };
                    _context.Admins.Add(manager);
                    createdCount++;
                }
                else
                {
                    skippedCount++;
                }

                _context.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = $"Đã tạo dữ liệu admin mẫu thành công! Tạo mới: {createdCount} admin, Bỏ qua: {skippedCount} admin đã tồn tại.",
                    created = createdCount,
                    skipped = skippedCount,
                    info = new {
                        admin = "admin@unistyle.com / admin123",
                        manager = "manager@unistyle.com / manager123",
                        note = "Có thể đăng nhập bằng Email hoặc FullName"
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Seed Sample Users - Tạo dữ liệu mẫu cho người dùng
        /// URL: /Admin/SeedUsers
        /// </summary>
        
        /// <summary>
        /// Comments Management - Duyệt bình luận
        /// URL: /Admin/Comments
        /// </summary>
        public IActionResult Comments()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            // Redirect to CommentsController Index
            return RedirectToAction("Index", "Comments");
        }
    }
}