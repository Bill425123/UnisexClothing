using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UnisexClothes.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace UnisexClothes.Controllers
{
    public class SpinWheelController : Controller
    {
        private readonly UniStyleDbContext _context;
        private readonly ILogger<SpinWheelController> _logger;

        public SpinWheelController(UniStyleDbContext context, ILogger<SpinWheelController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetVoucherPoolApi()
        {
            try
            {
                var pool = GetVoucherPool();
                
                // Tính thống kê
                var realPrizes = pool.Where(v => v.Type != "none").ToList();
                var stats = new
                {
                    totalPrizes = realPrizes.Count,
                    minValue = realPrizes.Any() ? realPrizes.Min(v => v.Value) : 0,
                    maxValue = realPrizes.Any() ? realPrizes.Max(v => v.Value) : 0,
                    totalSlots = pool.Count
                };
                
                return Json(new { 
                    success = true, 
                    vouchers = pool,
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher pool");
                return Json(new { success = false, message = "Không tải được danh sách phần thưởng" });
            }
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Vòng Quay Voucher";

            var customerId = GetCurrentCustomerId();
            var remainingSpins = GetRemainingSpins(customerId);
            var dailySpins = GetDailySpins(customerId);

            ViewBag.RemainingSpins = remainingSpins;
            ViewBag.DailySpins = dailySpins;
            ViewBag.IsLoggedIn = customerId.HasValue;

            return View();
        }

        public IActionResult MyVouchers()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetMyVouchers()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                List<VoucherModel> vouchers = new List<VoucherModel>();

                string sessionKey = customerId.HasValue ? $"MyVouchers_{customerId}" : "MyVouchers_Guest";
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                
                if (!string.IsNullOrEmpty(vouchersJson))
                {
                    var allVouchers = JsonSerializer.Deserialize<List<VoucherModel>>(vouchersJson) ?? new List<VoucherModel>();
                    
                    // Filter out expired vouchers and check if used in database
                    vouchers = allVouchers.Where(v => 
                    {
                        // Remove expired vouchers
                        if (v.ExpiryDate.HasValue && v.ExpiryDate.Value < DateTime.Now)
                            return false;
                        
                        // Check if voucher is used in database
                        if (!string.IsNullOrEmpty(v.Code))
                        {
                            var coupon = _context.Coupons
                                .FirstOrDefault(c => c.Code == v.Code.ToUpper());
                            
                            if (coupon != null && coupon.IsUsed == true)
                                return false;
                        }
                        
                        return true;
                    }).ToList();
                    
                    // Update session with filtered vouchers
                    if (allVouchers.Count != vouchers.Count)
                    {
                        HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(vouchers));
                    }
                }

                return Json(new { success = true, vouchers = vouchers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetVoucherCount()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                int count = 0;

                string sessionKey = customerId.HasValue ? $"MyVouchers_{customerId}" : "MyVouchers_Guest";
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                
                if (!string.IsNullOrEmpty(vouchersJson))
                {
                    var vouchers = JsonSerializer.Deserialize<List<VoucherModel>>(vouchersJson);
                    if (vouchers != null)
                    {
                        // Count only valid vouchers (not expired and not used)
                        count = vouchers.Count(v =>
                        {
                            // Check if expired
                            if (v.ExpiryDate.HasValue && v.ExpiryDate.Value < DateTime.Now)
                                return false;
                            
                            // Check if used in database
                            if (!string.IsNullOrEmpty(v.Code))
                            {
                                var coupon = _context.Coupons
                                    .FirstOrDefault(c => c.Code == v.Code.ToUpper());
                                
                                if (coupon != null && coupon.IsUsed == true)
                                    return false;
                            }
                            
                            return true;
                        });
                    }
                }

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveVoucherToCollection([FromBody] VoucherModel voucher)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                string sessionKey = customerId.HasValue ? $"MyVouchers_{customerId}" : "MyVouchers_Guest";

                // Get existing vouchers
                var vouchersJson = HttpContext.Session.GetString(sessionKey);
                List<VoucherModel> vouchers = new List<VoucherModel>();
                
                if (!string.IsNullOrEmpty(vouchersJson))
                {
                    vouchers = JsonSerializer.Deserialize<List<VoucherModel>>(vouchersJson) ?? new List<VoucherModel>();
                }

                // Add saved date
                voucher.ExpiryDate = DateTime.Now.AddDays(30);
                
                // Add to collection
                vouchers.Add(voucher);

                // Save back to session
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(vouchers));

                return Json(new { success = true, message = "Đã lưu voucher vào kho!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int? GetCurrentCustomerId()
        {
            // Ưu tiên đọc từ Session (được set khi người dùng đăng nhập thông qua AccountController)
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                return sessionUserId.Value;
            }

            // Fallback cho trường hợp đăng nhập thông qua cơ chế khác sử dụng Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int customerId))
            {
                return customerId;
            }

            return null;
        }

        private int GetRemainingSpins(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return 0;
            }

            var user = _context.Users.Find(customerId.Value);
            if (user == null) return 0;

            // Đảm bảo SpinNumber luôn là 3 nếu null hoặc <= 0
            if (user.SpinNumber == null || user.SpinNumber <= 0)
            {
                user.SpinNumber = 3;
                _context.SaveChanges();
            }

            return user.SpinNumber.Value;
        }

        private int GetDailySpins(int? customerId)
        {
            return 3; // Mặc định 3 lần/ngày
        }

        [HttpPost]
        public async Task<IActionResult> Spin()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (!customerId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "🔒 Vui lòng đăng nhập để sử dụng vòng quay voucher!",
                        requiresLogin = true
                    });
                }

                var remainingSpins = GetRemainingSpins(customerId);

                // Kiểm tra số lần quay còn lại
                if (remainingSpins <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "🎯 Bạn đã hết lượt quay hôm nay! Hãy quay lại vào ngày mai nhé!",
                        remainingSpins = remainingSpins
                    });
                }

                // Danh sách voucher với tỷ lệ trúng khác nhau
                var vouchers = GetVoucherPool();
                var selectedVoucher = SelectVoucherByProbability(vouchers);
                
                // Tìm index thực tế của voucher trong mảng
                var voucherIndex = vouchers.FindIndex(v => v.Id == selectedVoucher.Id);
                if (voucherIndex == -1) voucherIndex = 0; // Fallback
                
                _logger.LogInformation($"Selected voucher: ID={selectedVoucher.Id}, Name={selectedVoucher.Name}, Index={voucherIndex}, TotalVouchers={vouchers.Count}");

                // Giảm số lần quay
                var user = await _context.Users.FindAsync(customerId.Value);
                if (user != null)
                {
                    user.SpinNumber = Math.Max(0, user.SpinNumber.Value - 1);
                    await _context.SaveChangesAsync();
                }

                // Lưu voucher vào session nếu trúng
                if (selectedVoucher.Type != "none")
                {
                    HttpContext.Session.SetString("AppliedVoucher", JsonSerializer.Serialize(selectedVoucher));
                }

                // Tính góc quay với animation mượt mà - dùng INDEX không phải ID
                var finalAngle = CalculateSpinAngle(voucherIndex, vouchers.Count);

                var newRemainingSpins = GetRemainingSpins(customerId);

                _logger.LogInformation($"Spin completed for customer {customerId}: {selectedVoucher.Name}");

                _logger.LogInformation($"Selected voucher: {selectedVoucher.Name} ({selectedVoucher.Code})");

                return Json(new
                {
                    success = true,
                    voucher = selectedVoucher,
                    angle = finalAngle,
                    remainingSpins = newRemainingSpins,
                    message = GetSpinMessage(selectedVoucher),
                    animation = GetAnimationType(selectedVoucher)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Spin action");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra, vui lòng thử lại!"
                });
            }
        }

        private List<VoucherModel> GetVoucherPool()
        {
            // Lấy TẤT CẢ coupon đang hoạt động từ Admin (DB)
            var now = DateTime.Now;
            var activeCoupons = _context.Coupons
                .Where(c =>
                    (c.IsUsed == null || c.IsUsed == false) &&
                    (c.ExpiryDate == null || c.ExpiryDate >= now) &&
                    !string.IsNullOrEmpty(c.Code) &&
                    c.DiscountAmount.HasValue && c.DiscountAmount.Value > 0)
                .OrderByDescending(c => c.DiscountAmount) // Sắp xếp từ cao đến thấp
                .ThenByDescending(c => c.CreatedDate)
                .ToList(); // Lấy tất cả, không giới hạn!

            var vouchers = new List<VoucherModel>();
            int id = 1;

            // Thêm tất cả coupons từ DB
            foreach (var c in activeCoupons)
            {
                // Tính xác suất dựa trên giá trị (giá trị thấp hơn = xác suất cao hơn)
                int probability = 15;
                if (c.DiscountAmount.Value >= 500000) probability = 5;  // 500k: 5%
                else if (c.DiscountAmount.Value >= 300000) probability = 7;  // 300k: 7%
                else if (c.DiscountAmount.Value >= 200000) probability = 10; // 200k: 10%
                else if (c.DiscountAmount.Value >= 100000) probability = 12; // 100k+: 12%
                else probability = 15; // Dưới 100k: 15%

                // Chọn màu dựa vào giá trị
                string color = "#4facfe"; // Default blue
                if (c.DiscountAmount.Value >= 500000) color = "#FFD700"; // Vàng kim cho 500k
                else if (c.DiscountAmount.Value >= 300000) color = "#FF6B6B"; // Đỏ cho 300k
                else if (c.DiscountAmount.Value >= 200000) color = "#4ECDC4"; // Xanh ngọc cho 200k
                else if (c.DiscountAmount.Value >= 100000) color = "#95E1D3"; // Xanh lá nhạt cho 100k+
                else if (c.DiscountAmount.Value >= 50000) color = "#F38181"; // Hồng cho 50k+
                else color = "#C7CEEA"; // Xám nhạt cho dưới 50k

                vouchers.Add(new VoucherModel
                {
                    Id = id++,
                    Code = c.Code!,
                    Name = $"Giảm {c.DiscountAmount.Value:N0}đ",
                    Value = c.DiscountAmount.Value,
                    Type = "amount",
                    Color = color,
                    Probability = probability,
                    ExpiryDate = c.ExpiryDate
                });
            }

            // Chỉ thêm 1-2 ô "Chúc may mắn lần sau" nếu cần để vòng quay cân đối
            // Nếu số lượng voucher là số lẻ, thêm 1 ô để làm chẵn
            if (vouchers.Count > 0 && vouchers.Count % 2 != 0)
            {
                vouchers.Add(new VoucherModel
                {
                    Id = id++,
                    Code = "NONE",
                    Name = "Chúc may mắn lần sau",
                    Value = 0,
                    Type = "none",
                    Color = "#2C3E50",
                    Probability = 20 // Xác suất cao hơn để cân bằng
                });
            }

            // Nếu không có coupon nào từ DB, tạo fallback minimal
            if (vouchers.Count == 0)
            {
                _logger.LogWarning("No active coupons found in database, using fallback");
                vouchers.Add(new VoucherModel
                {
                    Id = 1,
                    Code = "NONE",
                    Name = "Chúc may mắn lần sau",
                    Value = 0,
                    Type = "none",
                    Color = "#2C3E50",
                    Probability = 100
                });
            }

            _logger.LogInformation($"Voucher pool created with {vouchers.Count} items from {activeCoupons.Count} active coupons");
            return vouchers;
        }

        private VoucherModel SelectVoucherByProbability(List<VoucherModel> vouchers)
        {
            var random = new Random();
            var totalProbability = vouchers.Sum(v => v.Probability);
            var randomNumber = random.Next(1, totalProbability + 1);

            var currentProbability = 0;
            foreach (var voucher in vouchers)
            {
                currentProbability += voucher.Probability;
                if (randomNumber <= currentProbability)
                {
                    return voucher;
                }
            }

            return vouchers.Last(); // Fallback
        }

        private double CalculateSpinAngle(int voucherIndex, int voucherCount)
        {
            var random = new Random();
            var spins = 5 + random.Next(3); // 5-7 vòng quay
            var sectorAngle = 360.0 / voucherCount; // Góc mỗi sector
            
            // QUAN TRỌNG: 
            // - Mũi tên cố định ở vị trí 12 giờ (top)
            // - Các ô được vẽ từ index 0 ở vị trí 12 giờ, theo chiều kim đồng hồ
            // - Vòng quay THUẬN chiều kim đồng hồ (rotate dương)
            
            // Để mũi tên chỉ vào GIỮA ô index X:
            // - Vị trí hiện tại của giữa ô X (tính từ top): X * sectorAngle + sectorAngle/2
            // - Để đưa nó về top, cần quay NGƯỢC lại: -(X * sectorAngle + sectorAngle/2)
            // - Vì muốn quay nhiều vòng, thêm N * 360°
            
            var positionAngle = voucherIndex * sectorAngle + (sectorAngle / 2); // Vị trí giữa ô X từ top
            var targetAngle = (spins * 360) - positionAngle; // Quay ngược lại
            
            // Normalize về 0-360 cho lần cuối
            var finalRotation = targetAngle % 360;
            if (finalRotation < 0) finalRotation += 360;

            _logger.LogInformation($"🎯 Spin: index={voucherIndex}/{voucherCount}, sector={sectorAngle:F2}°, position={positionAngle:F2}°, target={targetAngle:F2}°, final={finalRotation:F2}°");
            return targetAngle;
        }

        private string GetSpinMessage(VoucherModel voucher)
        {
            return voucher.Type switch
            {
                "none" => "🎯 Chúc may mắn lần sau! Hãy thử lại nhé!",
                "bonus" => "🎉 Chúc mừng! Bạn đã trúng quà tặng đặc biệt!",
                "freeship" => "🚚 Tuyệt vời! Bạn được miễn phí vận chuyển!",
                "percent" => $"🎊 Xuất sắc! Bạn được giảm {voucher.Value}% cho đơn hàng tiếp theo!",
                "amount" => $"💰 Hoàn hảo! Bạn được giảm {voucher.Value:N0}đ cho đơn hàng tiếp theo!",
                _ => "🎁 Chúc mừng bạn đã trúng thưởng!"
            };
        }

        private string GetAnimationType(VoucherModel voucher)
        {
            return voucher.Type switch
            {
                "none" => "shake",
                "bonus" => "confetti",
                "freeship" => "bounce",
                "percent" => "pulse",
                "amount" => "glow",
                _ => "fadeIn"
            };
        }

        [HttpPost]
        public IActionResult ApplyVoucher([FromBody] VoucherRequestModel model)
        {
            _logger.LogInformation($"ApplyVoucher called with code: {model?.Code}");

            if (model == null || string.IsNullOrEmpty(model.Code))
                return Json(new { success = false, message = "❌ Mã voucher không hợp lệ" });

            // Ưu tiên tìm coupon trong DB
            var now = DateTime.Now;
            var codeUpper = (model.Code ?? string.Empty).Trim().ToUpper();
            var coupon = _context.Coupons.FirstOrDefault(c =>
                c.Code != null &&
                c.Code.ToUpper() == codeUpper &&
                (c.IsUsed == null || c.IsUsed == false) &&
                (c.ExpiryDate == null || c.ExpiryDate >= now));

            VoucherModel? voucher = null;
            if (coupon != null && coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0)
            {
                voucher = new VoucherModel
                {
                    Id = coupon.CouponId,
                    Code = coupon.Code!,
                    Name = $"Giảm {coupon.DiscountAmount.Value:N0}đ",
                    Value = coupon.DiscountAmount.Value,
                    Type = "amount",
                    Color = "#4facfe",
                    Probability = 12,
                    ExpiryDate = coupon.ExpiryDate
                };
            }
            else
            {
                // Fallback: tìm trong pool tĩnh (nếu admin không có coupon khớp)
                var vouchers = GetVoucherPool();
                voucher = vouchers.FirstOrDefault(v => v.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase));
            }

            if (voucher == null)
            {
                _logger.LogWarning($"Voucher not found: {model.Code}");
                return Json(new { success = false, message = "❌ Mã voucher không tồn tại" });
            }

            // Cộng dồn nếu cùng mã đang tồn tại trong session
            var existingJson = HttpContext.Session.GetString("AppliedVoucher");
            if (!string.IsNullOrEmpty(existingJson))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<VoucherModel>(existingJson);
                    if (existing != null && existing.Code.Equals(voucher.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        // Cộng dồn theo loại
                        existing.TimesApplied += 1;
                        if (existing.Type == "amount")
                        {
                            existing.AccumulatedValue += voucher.Value;
                        }
                        else if (existing.Type == "percent")
                        {
                            existing.AccumulatedValue += voucher.Value; // tổng % (có thể hạn chế tối đa 100% ở lúc tính tiền)
                        }
                        else if (existing.Type == "freeship")
                        {
                            existing.AccumulatedValue = 1; // flag miễn phí ship
                        }

                        var mergedJson = JsonSerializer.Serialize(existing);
                        HttpContext.Session.SetString("AppliedVoucher", mergedJson);
                        _logger.LogInformation($"Voucher stacked: {existing.Name} x{existing.TimesApplied}, Accum = {existing.AccumulatedValue}");
                        return Json(new { success = true, message = $"✅ Đã cộng dồn {existing.Name} (x{existing.TimesApplied})!", voucher = existing });
                    }
                }
                catch { /* ignore parse errors and overwrite below */ }
            }

            // Nếu không trùng mã, ghi voucher mới
            voucher.TimesApplied = 1;
            voucher.AccumulatedValue = voucher.Value;
            var voucherJson = JsonSerializer.Serialize(voucher);
            HttpContext.Session.SetString("AppliedVoucher", voucherJson);
            _logger.LogInformation($"Voucher applied successfully: {voucher.Name} ({voucher.Code})");
            _logger.LogInformation($"Voucher JSON saved to session: {voucherJson}");

            return Json(new { success = true, message = $"✅ Áp dụng {voucher.Name} thành công!", voucher });
        }

        [HttpGet]
        public IActionResult TestSession()
        {
            var voucherJson = HttpContext.Session.GetString("AppliedVoucher");
            _logger.LogInformation($"TestSession - Voucher JSON: {voucherJson}");

            if (string.IsNullOrEmpty(voucherJson))
            {
                return Json(new { success = false, message = "No voucher in session" });
            }

            try
            {
                var voucher = JsonSerializer.Deserialize<VoucherModel>(voucherJson);
                return Json(new { success = true, voucher = voucher });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deserializing voucher: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetDailySpins()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                foreach (var user in users)
                {
                    user.SpinNumber = 3;
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily spins reset for all users");
                return Json(new { success = true, message = "✅ Đã reset số lần quay cho tất cả người dùng!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting daily spins");
                return Json(new { success = false, message = $"❌ Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetMySpins()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (!customerId.HasValue)
                {
                    return Json(new { success = false, message = "❌ Vui lòng đăng nhập để reset lượt quay!" });
                }

                var user = await _context.Users.FindAsync(customerId.Value);
                if (user != null)
                {
                    user.SpinNumber = 3;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Spins reset for user {customerId}");
                return Json(new { success = true, message = "✅ Đã reset số lần quay của bạn về 3!", remainingSpins = 3 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting customer spins");
                return Json(new { success = false, message = $"❌ Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetRemainingSpins()
        {
            var customerId = GetCurrentCustomerId();
            var remainingSpins = GetRemainingSpins(customerId);
            var dailySpins = GetDailySpins(customerId);

            return Json(new
            {
                remainingSpins = remainingSpins,
                dailySpins = dailySpins,
                isLoggedIn = customerId.HasValue
            });
        }

        [HttpGet]
        public IActionResult GetVoucherInfo()
        {
            var voucherJson = HttpContext.Session.GetString("AppliedVoucher");
            if (string.IsNullOrEmpty(voucherJson))
            {
                return Json(new { hasVoucher = false });
            }

            try
            {
                var voucher = JsonSerializer.Deserialize<VoucherModel>(voucherJson);
                return Json(new { hasVoucher = true, voucher = voucher });
            }
            catch
            {
                return Json(new { hasVoucher = false });
            }
        }

        public class VoucherModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public decimal Value { get; set; }
            public string Type { get; set; } = ""; // "amount", "percent", "freeship", "none"
            public string Color { get; set; } = "#4facfe";
            public int Probability { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public int TimesApplied { get; set; } = 0;
            public decimal AccumulatedValue { get; set; } = 0;
        }

        public class VoucherRequestModel
        {
            public string Code { get; set; } = "";
        }
    }
}