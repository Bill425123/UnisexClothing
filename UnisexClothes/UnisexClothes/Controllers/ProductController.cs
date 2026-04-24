using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;
using System.Linq;

namespace UnisexClothes.Controllers
{
    public class ProductController : Controller
    {
        private readonly UniStyleDbContext _context;
        private static readonly Dictionary<string, int> LegacyCategoryMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ao", 1 },
            { "quan", 2 },
            { "phukien", 3 }
        };

        public ProductController(UniStyleDbContext context)
        {
            _context = context;
        }

        // Trang Shop - Hiển thị danh sách sản phẩm
        public IActionResult Shop(string category = "all", string sortBy = "name", string keyword = "")
        {
            var categories = _context.Categories
                .Where(c => c.IsActive ?? true)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            ViewData["Category"] = category;
            ViewData["SortBy"] = sortBy;
            ViewData["Keyword"] = keyword ?? string.Empty;
            return View(categories);
        }

        // API: Lấy danh sách sản phẩm cho Shop với pagination
        [HttpGet]
        public IActionResult GetProducts(string category = "all", string sortBy = "name", string keyword = "", int page = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products.Where(p => p.IsActive ?? true);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var trimmedKeyword = keyword.Trim();
                    query = query.Where(p => EF.Functions.Like(p.ProductName, $"%{trimmedKeyword}%"));
                }

                // Filter by category
                if (TryResolveCategoryId(category, out var catId))
                {
                    query = query.Where(p => p.CategoryId == catId);
                }

                // Sort
                query = sortBy switch
                {
                    "price-asc" => query.OrderBy(p => p.Price),
                    "price-desc" => query.OrderByDescending(p => p.Price),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.ProductName)
                };

                // Get total count for pagination
                var totalCount = query.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Pagination
                var categoriesMap = _context.Categories.ToDictionary(c => c.CategoryId, c => c.CategoryName);

                var productPage = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var products = productPage
                    .Select(p => new
                    {
                        id = p.ProductId,
                        name = p.ProductName,
                        image = p.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                        price = p.Price,
                        discount = p.DiscountPercent ?? 0,
                        salePrice = p.Price * (1 - (decimal)(p.DiscountPercent ?? 0) / 100),
                        stock = p.StockQuantity,
                        rating = p.Rating,
                        categoryId = p.CategoryId,
                        categorySlug = $"cat-{p.CategoryId}",
                        category = categoriesMap.TryGetValue(p.CategoryId, out var catName) ? catName : "Danh mục khác"
                    })
                    .ToList();

                return Json(new 
                { 
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

        // Trang Product Detail - Chi tiết sản phẩm
        public IActionResult Detail(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // API: Lấy chi tiết sản phẩm
        [HttpGet]
        public IActionResult GetProductDetail(int id)
        {
            try
            {
                var productEntity = _context.Products
                    .FirstOrDefault(p => p.ProductId == id && (p.IsActive ?? true));

                if (productEntity == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                var categoryName = _context.Categories
                    .Where(c => c.CategoryId == productEntity.CategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault() ?? "Danh mục khác";

                var product = new
                {
                    id = productEntity.ProductId,
                    name = productEntity.ProductName,
                    image = productEntity.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                    price = productEntity.Price,
                    discount = productEntity.DiscountPercent ?? 0,
                    salePrice = productEntity.Price * (1 - (decimal)(productEntity.DiscountPercent ?? 0) / 100),
                    stock = productEntity.StockQuantity,
                    rating = productEntity.Rating,
                    category = categoryName,
                    description = productEntity.Description ?? "",
                    categoryId = productEntity.CategoryId,
                    categorySlug = $"cat-{productEntity.CategoryId}"
                };

                return Json(new { success = true, data = product });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
        
        private static bool TryResolveCategoryId(string? categoryParam, out int categoryId)
        {
            categoryId = 0;
            if (string.IsNullOrWhiteSpace(categoryParam) || categoryParam.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var trimmed = categoryParam.Trim();
            if (trimmed.StartsWith("cat-", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(4);
            }

            if (int.TryParse(trimmed, out categoryId))
            {
                return true;
            }

            if (LegacyCategoryMap.TryGetValue(trimmed, out categoryId))
            {
                return true;
            }

            return false;
        }

        // API: Tìm hoặc tạo variant cho product
        [HttpGet]
        public IActionResult FindOrCreateVariant(int productId, string size, string color)
        {
            try
            {
                // Tìm variant hiện có
                var variant = _context.ProductVariants
                    .FirstOrDefault(v => v.ProductId == productId && v.Size == size && v.Color == color);

                if (variant != null)
                {
                    return Json(new { success = true, variantId = variant.VariantId });
                }

                // Nếu không tìm thấy, tạo variant mới
                var newVariant = new ProductVariant
                {
                    ProductId = productId,
                    Size = size,
                    Color = color,
                    StockQuantity = 100, // Default stock
                    AdditionalPrice = 0 // Default no additional price
                };

                _context.ProductVariants.Add(newVariant);
                _context.SaveChanges();

                return Json(new { success = true, variantId = newVariant.VariantId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get comments for a product
        [HttpGet]
        public IActionResult GetComments(int productId, int skip = 0, int take = 5)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                int? currentCustomerId = null;
                
                // Get current customer ID if user is logged in
                if (userId.HasValue)
                {
                    var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
                    if (user != null)
                    {
                        var customer = _context.Customers.FirstOrDefault(c => c.Email == user.Email);
                        if (customer != null)
                        {
                            currentCustomerId = customer.CustomerId;
                        }
                    }
                }

                // Get all comments
                var allComments = _context.Comments
                    .Where(c => c.ProductId == productId)
                    .Include(c => c.Customer)
                    .OrderByDescending(c => c.CommentDate)
                    .ToList();

                // Filter: Show published comments OR current user's own comments
                var filteredComments = allComments
                    .Where(c => (c.IsPublished == true) || (currentCustomerId.HasValue && c.CustomerId == currentCustomerId.Value))
                    .Select(c => new
                    {
                        productId = c.ProductId,
                        customerId = c.CustomerId,
                        customerName = c.Customer.Name,
                        rating = c.Rating,
                        content = c.Content,
                        isPublished = c.IsPublished,
                        commentDate = c.CommentDate,
                        isMyComment = currentCustomerId.HasValue && c.CustomerId == currentCustomerId.Value
                    })
                    .ToList();

                var totalCount = filteredComments.Count;
                var comments = filteredComments.Skip(skip).Take(take).ToList();

                return Json(new { 
                    success = true, 
                    comments, 
                    totalCount,
                    hasMore = skip + take < totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Add comment for a product
        [HttpPost]
        public IActionResult AddComment([FromBody] CommentRequest request)
        {
            try
            {
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận!" });
                }

                // Get customer ID from Users table
                var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
                }

                // Find or create customer
                var customer = _context.Customers.FirstOrDefault(c => c.Email == user.Email);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = user.FullName,
                        Email = user.Email,
                        Phone = user.PhoneNumber,
                        CreatedAt = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                    _context.SaveChanges();
                }

                // Check if already commented
                var existingComment = _context.Comments
                    .FirstOrDefault(c => c.ProductId == request.ProductId && c.CustomerId == customer.CustomerId);

                if (existingComment != null)
                {
                    return Json(new { success = false, message = "Bạn đã bình luận sản phẩm này rồi!" });
                }

                // Add new comment
                var comment = new Comment
                {
                    ProductId = request.ProductId,
                    CustomerId = customer.CustomerId,
                    Rating = request.Rating,
                    Content = request.Content,
                    CommentDate = DateTime.Now,
                    IsPublished = false // Pending approval
                };

                _context.Comments.Add(comment);
                _context.SaveChanges();

                // Return comment info for immediate display
                return Json(new { 
                    success = true, 
                    message = "Cảm ơn bạn đã đánh giá!",
                    comment = new
                    {
                        productId = comment.ProductId,
                        customerId = comment.CustomerId,
                        customerName = customer.Name,
                        rating = comment.Rating,
                        content = comment.Content,
                        isPublished = comment.IsPublished,
                        commentDate = comment.CommentDate,
                        isMyComment = true
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to get current user ID
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }
    }

    // Request model for AddComment
    public class CommentRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
    }
}
