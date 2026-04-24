using Microsoft.AspNetCore.Mvc;
using UnisexClothes.Models;

namespace UnisexClothes.Controllers
{
    /// <summary>
    /// Controller để test kết nối database
    /// Truy cập: http://localhost:5000/Test/Connection
    /// </summary>
    public class TestController : Controller
    {
        private readonly UniStyleDbContext _context;

        public TestController(UniStyleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Test database connection
        /// URL: /Test/Connection
        /// </summary>
        public IActionResult Connection()
        {
            ViewBag.ConnectionString = _context.GetConnectionInfo();
            ViewBag.IsConnected = _context.TestConnection();
            ViewBag.ServerTime = DateTime.Now;

            return View();
        }

        /// <summary>
        /// API endpoint để test connection
        /// URL: /Test/CheckConnection
        /// </summary>
        [HttpGet]
        public IActionResult CheckConnection()
        {
            try
            {
                var isConnected = _context.TestConnection();
                
                return Json(new
                {
                    success = isConnected,
                    message = isConnected 
                        ? "✅ Kết nối database thành công!" 
                        : "❌ Không thể kết nối database!",
                    connectionString = _context.GetConnectionInfo(),
                    serverTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"❌ Lỗi: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }
    }
}



