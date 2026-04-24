using Microsoft.AspNetCore.Mvc;
using UnisexClothes.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace UnisexClothes.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly UniStyleDbContext _context;

        public CheckoutController(UniStyleDbContext context)
        {
            _context = context;
        }

        // Trang Checkout - Thanh toán
        public IActionResult Index()
        {
            // Kiểm tra đăng nhập - BẮT BUỘC phải đăng nhập để thanh toán
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Lưu returnUrl để redirect về sau khi đăng nhập
                HttpContext.Session.SetString("ReturnUrl", "/Checkout/Index");
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // API: Lấy thông tin user hiện tại để tự động điền form
        [HttpGet]
        public IActionResult GetUserInfo()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var user = _context.Users.Find(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin user" });
                }

                // Parse địa chỉ nếu có (format: "Địa chỉ chi tiết, Quận/Huyện, Tỉnh/Thành phố")
                // Hoặc format: "Số nhà, tên đường, phường/xã, Quận/Huyện, Tỉnh/Thành phố"
                string? province = null;
                string? district = null;
                string? addressDetail = null;

                if (!string.IsNullOrEmpty(user.Address))
                {
                    var addressParts = user.Address.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToArray();
                    
                    if (addressParts.Length >= 3)
                    {
                        // Lấy phần cuối là province
                        province = addressParts[addressParts.Length - 1];
                        // Phần trước đó là district
                        district = addressParts[addressParts.Length - 2];
                        // Các phần còn lại là địa chỉ chi tiết
                        addressDetail = string.Join(", ", addressParts.Take(addressParts.Length - 2));
                    }
                    else if (addressParts.Length == 2)
                    {
                        // Có thể là "Địa chỉ, Tỉnh" hoặc "Quận, Tỉnh"
                        // Thử đoán: nếu phần 2 có từ "TP", "Tỉnh", "Thành phố" thì là province
                        if (addressParts[1].Contains("TP") || addressParts[1].Contains("Tỉnh") || 
                            addressParts[1].Contains("Thành phố") || addressParts[1].Contains("Hà Nội") ||
                            addressParts[1].Contains("HCM") || addressParts[1].Contains("Đà Nẵng"))
                        {
                            province = addressParts[1];
                            addressDetail = addressParts[0];
                        }
                        else
                        {
                            // Có thể là "Địa chỉ, Quận"
                            district = addressParts[1];
                            addressDetail = addressParts[0];
                        }
                    }
                    else if (addressParts.Length == 1)
                    {
                        addressDetail = addressParts[0];
                    }
                }

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        fullName = user.FullName,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber ?? "",
                        address = user.Address ?? "",
                        province = province,
                        district = district,
                        addressDetail = addressDetail
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Tính phí vận chuyển dựa trên giá trị đơn hàng
        [HttpGet]
        public IActionResult CalculateShippingFee(decimal orderAmount)
        {
            try
            {
                // Lấy phí ship cho đơn < 500k
                var standardFee = _context.Fees
                    .FirstOrDefault(f => f.Name.Contains("tiêu chuẩn") && f.Name.Contains("vận chuyển"));

                // Lấy phí miễn phí cho đơn >= 500k
                var freeFee = _context.Fees
                    .FirstOrDefault(f => f.Name.Contains("Miễn phí") && f.Name.Contains("vận chuyển"));

                // Logic: >= 500k thì miễn phí, < 500k thì tính phí
                if (orderAmount >= 500000 && freeFee != null)
                {
                    return Json(new
                    {
                        success = true,
                        fee = 0,
                        feeName = freeFee.Name,
                        isFree = true
                    });
                }
                else if (standardFee != null)
                {
                    return Json(new
                    {
                        success = true,
                        fee = standardFee.Value ?? 30000,
                        feeName = standardFee.Name,
                        isFree = false
                    });
                }

                // Mặc định phí ship 30,000₫ nếu không tìm thấy
                return Json(new
                {
                    success = true,
                    fee = 30000,
                    feeName = "Phí vận chuyển tiêu chuẩn",
                    isFree = false
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, fee = 0 });
            }
        }

        // API: Lấy phí VAT
        [HttpGet]
        public IActionResult GetVAT()
        {
            try
            {
                var vat = _context.Fees
                    .FirstOrDefault(f => f.Name.Contains("VAT"));

                if (vat != null && vat.Value.HasValue)
                {
                    return Json(new
                    {
                        success = true,
                        percentage = vat.Value.Value,
                        name = vat.Name
                    });
                }

                return Json(new { success = true, percentage = 0, name = "VAT" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, percentage = 0 });
            }
        }

        // API: Lấy danh sách voucher có sẵn của user
        [HttpGet]
        public IActionResult GetMyVouchers()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                string sessionKey = userId.HasValue ? $"MyVouchers_{userId}" : "MyVouchers_Guest";
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                
                if (string.IsNullOrEmpty(vouchersJson))
                {
                    return Json(new { success = true, vouchers = new List<VoucherSessionModel>() });
                }

                var vouchers = System.Text.Json.JsonSerializer.Deserialize<List<VoucherSessionModel>>(vouchersJson);
                
                // Filter out expired and already used vouchers
                var validVouchers = vouchers?.Where(v => 
                    v.ExpiryDate.HasValue && v.ExpiryDate.Value >= DateTime.Now
                ).ToList() ?? new List<VoucherSessionModel>();

                return Json(new { success = true, vouchers = validVouchers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, vouchers = new List<VoucherSessionModel>() });
            }
        }

        // Xử lý thanh toán và lưu đơn hàng vào database
        [HttpPost]
        public IActionResult ProcessCheckout([FromBody] CheckoutRequest request)
        {
            try
            {
                // Kiểm tra đăng nhập - BẮT BUỘC phải đăng nhập để đặt hàng
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để đặt hàng!",
                        requiresLogin = true
                    });
                }

                if (request == null || request.CartItems == null || !request.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống!" });
                }

                // Build full shipping address
                string shippingAddress = $"{request.Address}, {request.District}, {request.Province}";

                // Calculate totals
                decimal subtotal = 0;
                foreach (var item in request.CartItems)
                {
                    subtotal += item.Price * item.Quantity;
                }

                decimal discountAmount = request.DiscountAmount ?? 0;
                decimal shippingFee = request.ShippingFee ?? 0;
                decimal totalAmount = subtotal - discountAmount + shippingFee;

                // Process voucher if provided
                int? couponId = null;
                if (!string.IsNullOrEmpty(request.VoucherCode))
                {
                    var coupon = _context.Coupons
                        .FirstOrDefault(c => c.Code == request.VoucherCode.ToUpper() && c.IsUsed == false);
                    
                    if (coupon != null && coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value >= DateTime.Now)
                    {
                        // Mark voucher as used
                        coupon.IsUsed = true;
                        coupon.UsedDate = DateTime.Now;
                        couponId = coupon.CouponId;
                    }
                }

                // Create Order
                var order = new Order
                {
                    UserId = userId,
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.Phone,
                    ShippingAddress = shippingAddress,
                    SubTotal = subtotal,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount,
                    PaymentMethod = GetPaymentMethodText(request.PaymentMethod),
                    OrderStatus = "Pending", // Chờ xác nhận
                    OrderDate = DateTime.Now,
                    Notes = request.Note,
                    CouponId = couponId
                };

                _context.Orders.Add(order);
                _context.SaveChanges(); // Save to get OrderId

                // Create OrderDetails
                foreach (var item in request.CartItems)
                {
                    // Get product info from database
                    var product = _context.Products.Find(item.ProductId);
                    if (product == null) continue;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        ProductImage = product.ProductImage,
                        Color = item.Color,
                        Size = item.Size,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalPrice = item.Price * item.Quantity
                    };

                    _context.OrderDetails.Add(orderDetail);

                    // Update stock quantity
                    if (product.StockQuantity.HasValue)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (product.StockQuantity < 0)
                            product.StockQuantity = 0;
                    }
                }

                _context.SaveChanges();

                // Clear cart from database if user is logged in
                if (userId.HasValue)
                {
                    var cartItems = _context.Carts.Where(c => c.UserId == userId.Value).ToList();
                    _context.Carts.RemoveRange(cartItems);
                    _context.SaveChanges();
                }
                else
                {
                    // Clear session cart for guest users
                    HttpContext.Session.Remove("SessionCart");
                }

                // Remove used voucher from session collection
                if (!string.IsNullOrEmpty(request.VoucherCode))
                {
                    RemoveVoucherFromSession(request.VoucherCode, userId);
                }

                return Json(new { 
                    success = true, 
                    message = "Đơn hàng đã được đặt thành công!",
                    orderId = order.OrderId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Trang xác nhận đơn hàng - Hiển thị bill
        public IActionResult OrderConfirmation(int? orderId)
        {
            if (orderId.HasValue)
            {
                ViewBag.OrderId = orderId.Value;
            }
            return View();
        }

        // Helper method to convert payment method code to text
        private string GetPaymentMethodText(string paymentMethod)
        {
            return paymentMethod switch
            {
                "cod" => "COD",
                "bankTransfer" => "Bank Transfer",
                "card" => "Credit Card",
                _ => "COD"
            };
        }

        // Helper method to remove voucher from session collection
        private void RemoveVoucherFromSession(string voucherCode, int? userId)
        {
            try
            {
                string sessionKey = userId.HasValue ? $"MyVouchers_{userId}" : "MyVouchers_Guest";
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                
                if (!string.IsNullOrEmpty(vouchersJson))
                {
                    var vouchers = System.Text.Json.JsonSerializer.Deserialize<List<VoucherSessionModel>>(vouchersJson);
                    if (vouchers != null)
                    {
                        // Remove the used voucher
                        vouchers.RemoveAll(v => v.Code?.Equals(voucherCode, StringComparison.OrdinalIgnoreCase) == true);
                        
                        // Save back to session
                        HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(vouchers));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the checkout
                Console.WriteLine($"Error removing voucher from session: {ex.Message}");
            }
        }
    }

    // Voucher model for session storage
    public class VoucherSessionModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal Value { get; set; }
        public string? Type { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    // Request model for checkout
    public class CheckoutRequest
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public decimal? DiscountAmount { get; set; }
        public decimal? ShippingFee { get; set; }
        public string? VoucherCode { get; set; }
        public List<CartItemRequest> CartItems { get; set; } = new List<CartItemRequest>();
    }

    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}

