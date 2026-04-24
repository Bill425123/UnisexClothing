using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;
using System.Text.Json;

namespace UnisexClothes.Controllers
{
    public class CartController : Controller
    {
        private readonly UniStyleDbContext _context;
        private const string SessionCartKey = "SessionCart";

        public CartController(UniStyleDbContext context)
        {
            _context = context;
        }

        // Trang Cart - Giỏ hàng
        public IActionResult Index()
        {
            return View();
        }

        #region Helper - User & Session Cart
        private int? GetUserId()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return null;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            return user?.UserId;
        }

        private List<CartSessionItem> GetSessionCart()
        {
            var sessionData = HttpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(sessionData))
                return new List<CartSessionItem>();

            return JsonSerializer.Deserialize<List<CartSessionItem>>(sessionData)!;
        }

        private void SaveSessionCart(List<CartSessionItem> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(SessionCartKey, json);
        }

        public class CartSessionItem
        {
            public int ProductId { get; set; }
            public int? VariantId { get; set; }
            public int Quantity { get; set; }
        }
        #endregion

        // API: Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Kiểm tra đăng nhập - BẮT BUỘC phải đăng nhập để mua hàng
                var userId = GetUserId();
                if (userId == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng!",
                        requiresLogin = true
                    });
                }

                if (request == null || request.ProductId <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

                int productId = request.ProductId;
                int quantity = request.Quantity > 0 ? request.Quantity : 1;
                int? variantId = request.VariantId;

                // Kiểm tra sản phẩm có tồn tại và đang active không
                var product = _context.Products
                    .FirstOrDefault(p => p.ProductId == productId && (p.IsActive ?? true));

                if (product == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

                // Kiểm tra stock
                int availableStock = product.StockQuantity ?? 0;
                if (variantId.HasValue)
                {
                    var variant = _context.ProductVariants
                        .FirstOrDefault(v => v.VariantId == variantId.Value && v.ProductId == productId);
                    if (variant != null)
                        availableStock = variant.StockQuantity ?? 0;
                }

                if (availableStock < quantity)
                    return Json(new { success = false, message = $"Số lượng tồn kho không đủ! Chỉ còn {availableStock} sản phẩm." });

                // Đăng nhập → thêm vào DB
                var existingCartItem = _context.Carts
                    .FirstOrDefault(c => c.UserId == userId.Value
                        && c.ProductId == productId
                        && c.VariantId == variantId);

                if (existingCartItem != null)
                {
                    var newQuantity = existingCartItem.Quantity + quantity;
                    if (newQuantity > availableStock)
                        return Json(new { success = false, message = $"Số lượng tồn kho không đủ! Tối đa {availableStock} sản phẩm." });

                    existingCartItem.Quantity = newQuantity;
                    existingCartItem.AddedAt = DateTime.Now;
                }
                else
                {
                    _context.Carts.Add(new Cart
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        VariantId = variantId,
                        Quantity = quantity,
                        AddedAt = DateTime.Now
                    });
                }
                _context.SaveChanges();
            
            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!" });
        }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Lấy danh sách sản phẩm trong giỏ hàng
        [HttpGet]
        public IActionResult GetCartItems()
        {
            try
            {
                var userId = GetUserId();
                var cartItems = new List<object>();

                if (userId != null)
                {
                    cartItems = _context.Carts
                        .Where(c => c.UserId == userId.Value)
                        .Include(c => c.Product)
                        .Include(c => c.Variant)
                        .OrderByDescending(c => c.AddedAt)
                        .Select(c => new
                        {
                            cartId = c.CartId,
                            productId = c.ProductId,
                            productName = c.Product.ProductName,
                            productImage = c.Product.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                            price = c.Product.Price,
                            discount = c.Product.DiscountPercent ?? 0,
                            salePrice = c.Product.Price * (1 - (decimal)(c.Product.DiscountPercent ?? 0) / 100),
                            quantity = c.Quantity,
                            variantId = c.VariantId,
                            variantColor = c.Variant != null ? c.Variant.Color : null,
                            variantSize = c.Variant != null ? c.Variant.Size : null,
                            variantAdditionalPrice = c.Variant != null ? (c.Variant.AdditionalPrice ?? 0) : 0,
                            stock = c.Variant != null ? (c.Variant.StockQuantity ?? 0) : (c.Product.StockQuantity ?? 0),
                            addedAt = c.AddedAt
                        })
                        .ToList<object>();
                }
                else
                {
                    // Chưa đăng nhập → lấy từ session
                    var sessionCart = GetSessionCart();
                    cartItems = sessionCart.Select(c =>
                    {
                        var product = _context.Products.FirstOrDefault(p => p.ProductId == c.ProductId && (p.IsActive ?? true));
                        var variant = c.VariantId.HasValue ? _context.ProductVariants.FirstOrDefault(v => v.VariantId == c.VariantId) : null;
                        if (product == null) return null;

                        // Tạo unique cartId cho session: session_{ProductId}_{VariantId || 0}
                        // Convert sang số âm để phân biệt với cartId từ DB (luôn dương)
                        int sessionCartId = -(c.ProductId * 10000 + (c.VariantId ?? 0));

                        return new
                        {
                            cartId = sessionCartId,
                            productId = product.ProductId,
                            productName = product.ProductName,
                            productImage = product.ProductImage ?? "/images/HinhSanPhamUnisex/default.jpg",
                            price = product.Price,
                            discount = product.DiscountPercent ?? 0,
                            salePrice = product.Price * (1 - (decimal)(product.DiscountPercent ?? 0) / 100),
                            quantity = c.Quantity,
                            variantId = c.VariantId,
                            variantColor = variant != null ? variant.Color : null,
                            variantSize = variant != null ? variant.Size : null,
                            variantAdditionalPrice = variant != null ? (variant.AdditionalPrice ?? 0) : 0,
                            stock = variant != null ? (variant.StockQuantity ?? 0) : (product.StockQuantity ?? 0),
                            addedAt = DateTime.Now
                        };
                    }).Where(x => x != null).ToList<object>();
                }

                return Json(new { success = true, data = cartItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

                var userId = GetUserId();

                if (userId != null)
                {
                    var cartItem = _context.Carts
                        .Include(c => c.Product)
                        .Include(c => c.Variant)
                        .FirstOrDefault(c => c.CartId == request.CartId && c.UserId == userId.Value);

                    if (cartItem == null)
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });

                    int availableStock = cartItem.Product.StockQuantity ?? 0;
                    if (cartItem.VariantId.HasValue && cartItem.Variant != null)
                        availableStock = cartItem.Variant.StockQuantity ?? 0;

                    if (request.Quantity > availableStock)
                        return Json(new { success = false, message = $"Số lượng tồn kho không đủ! Chỉ còn {availableStock} sản phẩm." });

                    cartItem.Quantity = request.Quantity;
                    _context.SaveChanges();
                }
                else
                {
                    // Session cart - CartId là số âm: session_{ProductId}_{VariantId}
                    // Parse lại: cartId = -(ProductId * 10000 + VariantId)
                    var sessionCart = GetSessionCart();
                    int cartId = request.CartId;
                    
                    if (cartId >= 0)
                    {
                        return Json(new { success = false, message = "CartId không hợp lệ cho session cart!" });
                    }
                    
                    // Parse cartId: cartId = -(ProductId * 10000 + VariantId)
                    int temp = -cartId;
                    int productId = temp / 10000;
                    int variantId = temp % 10000;
                    
                    var item = sessionCart.FirstOrDefault(c => c.ProductId == productId && (c.VariantId ?? 0) == variantId);
                    if (item != null)
                    {
                        // Kiểm tra stock
                        var product = _context.Products.FirstOrDefault(p => p.ProductId == productId && (p.IsActive ?? true));
                        if (product == null)
                            return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
                        
                        int availableStock = product.StockQuantity ?? 0;
                        if (variantId > 0)
                        {
                            var variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == variantId && v.ProductId == productId);
                            if (variant != null)
                                availableStock = variant.StockQuantity ?? 0;
                        }
                        
                        if (request.Quantity > availableStock)
                            return Json(new { success = false, message = $"Số lượng tồn kho không đủ! Chỉ còn {availableStock} sản phẩm." });
                        
                        item.Quantity = request.Quantity;
                        SaveSessionCart(sessionCart);
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
                    }
                }

                return Json(new { success = true, message = "Đã cập nhật số lượng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                var userId = GetUserId();

                if (userId != null)
                {
                    // Xóa từ database
                    var cartItem = _context.Carts.FirstOrDefault(c => c.CartId == request.CartId && c.UserId == userId.Value);
                    if (cartItem != null)
                    {
                        _context.Carts.Remove(cartItem);
                        _context.SaveChanges();
                        return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
                    }
                }
                else
                {
                    // Xóa từ session cart (chưa login)
                    // CartId là số âm: session_{ProductId}_{VariantId}
                    var sessionCart = GetSessionCart();
                    int cartId = request.CartId;
                    
                    if (cartId >= 0)
                    {
                        return Json(new { success = false, message = "CartId không hợp lệ cho session cart!" });
                    }
                    
                    // Parse cartId: cartId = -(ProductId * 10000 + VariantId)
                    int temp = -cartId;
                    int productId = temp / 10000;
                    int variantId = temp % 10000;
                    
                    var item = sessionCart.FirstOrDefault(c => c.ProductId == productId && (c.VariantId ?? 0) == variantId);
                    if (item != null)
                    {
                        sessionCart.Remove(item);
                        SaveSessionCart(sessionCart);
                        return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Lấy số lượng loại sản phẩm trong giỏ hàng (dùng cho header)
        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                var userId = GetUserId();
                int count = 0;

                if (userId != null)
                    count = _context.Carts.Where(c => c.UserId == userId.Value).Count(); // Đếm số loại sản phẩm
                else
                    count = GetSessionCart().Count(); // Đếm số loại sản phẩm trong session

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}", count = 0 });
            }
        }

        // API: Xóa tất cả sản phẩm khỏi giỏ hàng
        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                var userId = GetUserId();
                if (userId != null)
                {
                    var cartItems = _context.Carts.Where(c => c.UserId == userId.Value).ToList();
                    _context.Carts.RemoveRange(cartItems);
                    _context.SaveChanges();
                }
                else
                {
                    HttpContext.Session.Remove(SessionCartKey);
                }

                return Json(new { success = true, message = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Validate voucher code
        [HttpGet]
        public IActionResult ValidateVoucher(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã voucher!" });
                }

                // Check if cart has items
                var userId = GetUserId();
                bool hasCartItems = false;

                if (userId != null)
                {
                    hasCartItems = _context.Carts.Any(c => c.UserId == userId.Value);
                }
                else
                {
                    hasCartItems = GetSessionCart().Any();
                }

                if (!hasCartItems)
                {
                    return Json(new { success = false, message = "Giỏ hàng trống! Vui lòng thêm sản phẩm trước khi áp dụng voucher." });
                }

                // Check in Coupon table
                var coupon = _context.Coupons
                    .FirstOrDefault(c => c.Code == code.ToUpper() && c.IsUsed == false);

                if (coupon == null)
                {
                    return Json(new { success = false, message = "Mã voucher không hợp lệ hoặc đã được sử dụng!" });
                }

                // Check expiry date
                if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.Now)
                {
                    return Json(new { success = false, message = "Mã voucher đã hết hạn!" });
                }

                // Return voucher info
                var voucherInfo = new
                {
                    code = coupon.Code,
                    name = $"Giảm {FormatCurrency(coupon.DiscountAmount ?? 0)}",
                    value = coupon.DiscountAmount ?? 0,
                    type = "amount", // Assuming amount type for now
                    expiryDate = coupon.ExpiryDate
                };

                return Json(new { success = true, voucher = voucherInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Get My Vouchers from session
        [HttpGet]
        public IActionResult GetMyVouchers()
        {
            try
            {
                int? userId = GetUserId();
                string sessionKey = userId.HasValue ? $"MyVouchers_{userId}" : "MyVouchers_Guest";
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                
                if (string.IsNullOrEmpty(vouchersJson))
                {
                    return Json(new { success = true, vouchers = new List<object>() });
                }

                var vouchers = System.Text.Json.JsonSerializer.Deserialize<List<VoucherSessionModel>>(vouchersJson);
                
                // Filter out expired vouchers
                var validVouchers = vouchers?.Where(v => 
                    v.ExpiryDate.HasValue && v.ExpiryDate.Value >= DateTime.Now
                ).ToList() ?? new List<VoucherSessionModel>();

                return Json(new { success = true, vouchers = validVouchers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, vouchers = new List<object>() });
            }
        }

        private string FormatCurrency(decimal? amount)
        {
            if (!amount.HasValue) return "0₫";
            return string.Format("{0:N0}₫", amount.Value);
        }
    }

    // Request models
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? VariantId { get; set; }
    }

    public class UpdateQuantityRequest
    {
        public int CartId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveFromCartRequest
    {
        public int CartId { get; set; }
    }
}
