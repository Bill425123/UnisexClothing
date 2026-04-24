using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;
using UnisexClothes.ViewModels;

namespace UnisexClothes.Controllers
{
    public class CommentsController : Controller
    {
        private readonly UniStyleDbContext _context;

        public CommentsController(UniStyleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check if admin is logged in
        /// </summary>
        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("AdminLoggedIn") == "true";
        }

        // Trang quản lý bình luận - Hiển thị danh sách sản phẩm có bình luận
        public async Task<IActionResult> Index(string? searchProduct, int? categoryId, int page = 1)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Admin");

            const int pageSize = 10;
            if (page < 1)
            {
                page = 1;
            }

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Comments)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchProduct))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(searchProduct));
            }

            var productSummariesRaw = await productsQuery
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    PrimaryImage = p.ProductImages
                        .OrderBy(pi => pi.DisplayOrder ?? int.MaxValue)
                        .ThenBy(pi => pi.ImageId)
                        .Select(pi => pi.ImageUrl)
                        .FirstOrDefault() ?? p.ProductImage,
                    CategoryName = p.Category.CategoryName,
                    p.CategoryId,
                    TotalComments = p.Comments.Count(),
                    ApprovedComments = p.Comments.Count(c => c.IsPublished == true),
                    PendingComments = p.Comments.Count(c => c.IsPublished != true),
                    AverageRating = p.Comments.Any()
                        ? p.Comments.Average(c => (double?)c.Rating)
                        : null
                })
                .OrderByDescending(p => p.TotalComments)
                .ToListAsync();

            var productSummaries = productSummariesRaw
                .Select(p => new CommentProductSummaryViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Brand = ExtractBrandFromProductName(p.ProductName),
                    CategoryName = p.CategoryName,
                    ThumbnailUrl = string.IsNullOrWhiteSpace(p.PrimaryImage)
                        ? "/images/LogovsBrandingUniSex/logo.png"
                        : p.PrimaryImage,
                    TotalComments = p.TotalComments,
                    ApprovedComments = p.ApprovedComments,
                    PendingComments = p.PendingComments,
                    AverageRating = p.AverageRating.HasValue
                        ? Math.Round(p.AverageRating.Value, 1)
                        : null
                })
                .ToList();

            var totalProducts = productSummaries.Count;
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var totalComments = productSummaries.Sum(p => p.TotalComments);
            var totalApproved = productSummaries.Sum(p => p.ApprovedComments);
            var totalPending = productSummaries.Sum(p => p.PendingComments);

            var pagedProducts = productSummaries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var viewModel = new CommentManagementViewModel
            {
                SearchProduct = searchProduct,
                CategoryId = categoryId,
                Categories = categories,
                Products = pagedProducts,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalProducts = totalProducts,
                TotalComments = totalComments,
                TotalApproved = totalApproved,
                TotalPending = totalPending
            };

            return View(viewModel);
        }

        // Chi tiết bình luận cho một sản phẩm
        public async Task<IActionResult> Details(int productId, bool? isPublished)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Admin");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return NotFound();
            }

            var commentsQuery = _context.Comments
                .Include(c => c.Customer)
                .Include(c => c.Product)
                .Where(c => c.ProductId == productId)
                .AsQueryable();

            // Filter by publish status
            if (isPublished.HasValue)
            {
                commentsQuery = commentsQuery.Where(c => c.IsPublished == isPublished.Value);
            }

            var comments = await commentsQuery
                .OrderByDescending(c => c.CommentDate)
                .ToListAsync();

            ViewBag.Product = product;
            ViewBag.IsPublished = isPublished;

            return View(comments);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int productId, int customerId)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.CustomerId == customerId);

            if (comment == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Không tìm thấy bình luận" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                return RedirectToAction(nameof(Details), new { productId });
            }

            comment.IsPublished = true;

            try
            {
                await _context.SaveChangesAsync();
                
                // Nếu là AJAX request, trả về JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đã phê duyệt bình luận!" });
                }
                
                // Nếu là request thông thường, redirect về trang Details
                TempData["SuccessMessage"] = "Đã phê duyệt bình luận thành công!";
                return RedirectToAction(nameof(Details), new { productId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Details), new { productId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int productId, int customerId)
        {
            if (!IsAdminLoggedIn())
                return Json(new { success = false, message = "Unauthorized" });

            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.CustomerId == customerId);

            if (comment == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Không tìm thấy bình luận" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                return RedirectToAction(nameof(Details), new { productId });
            }

            comment.IsPublished = false;

            try
            {
                await _context.SaveChangesAsync();
                
                // Nếu là AJAX request, trả về JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đã ẩn bình luận!" });
                }
                
                // Nếu là request thông thường, redirect về trang Details
                TempData["SuccessMessage"] = "Đã ẩn bình luận thành công!";
                return RedirectToAction(nameof(Details), new { productId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Details), new { productId });
            }
        }

        public async Task<IActionResult> Create(int? productId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Admin");

            ViewBag.Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductId, p.ProductName })
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .Select(c => new { c.CustomerId, c.Name, c.Email })
                .ToListAsync();

            var comment = new Comment
            {
                ProductId = productId ?? 0,
                CommentDate = DateTime.Now,
                IsPublished = true
            };

            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comment comment)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Admin");

            // Check if comment already exists
            var existingComment = await _context.Comments
                .FirstOrDefaultAsync(c => c.ProductId == comment.ProductId && c.CustomerId == comment.CustomerId);

            if (existingComment != null)
            {
                ModelState.AddModelError("", "Khách hàng này đã bình luận sản phẩm này rồi!");
                
                ViewBag.Products = await _context.Products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new { p.ProductId, p.ProductName })
                    .ToListAsync();

                ViewBag.Customers = await _context.Customers
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.CustomerId, c.Name, c.Email })
                    .ToListAsync();

                return View(comment);
            }

            // Get IsPublished from form (checkbox returns "true" when checked, "false" when unchecked)
            var isPublishedValues = Request.Form["IsPublished"];
            bool? isPublished = isPublishedValues.Contains("true") ? true : false;

            comment.CommentDate = DateTime.Now;
            comment.IsPublished = isPublished;

            try
            {
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo bình luận thành công!";
                return RedirectToAction(nameof(Details), new { productId = comment.ProductId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể tạo bình luận. Vui lòng thử lại.");
            }

            ViewBag.Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductId, p.ProductName })
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .Select(c => new { c.CustomerId, c.Name, c.Email })
                .ToListAsync();

            return View(comment);
        }

        private static string ExtractBrandFromProductName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return "Chưa cập nhật";
            }

            var separators = new[] { " for ", " For ", " FOR " };
            foreach (var separator in separators)
            {
                if (productName.Contains(separator, StringComparison.OrdinalIgnoreCase))
                {
                    return productName.Split(separator, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }
            }

            var parts = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0]} {parts[1]}";
            }

            return parts.Length == 1 ? parts[0] : "Chưa cập nhật";
        }
    }
}
