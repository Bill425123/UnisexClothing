using Microsoft.EntityFrameworkCore;

namespace UnisexClothes.Models;

public static class UniStyleDbContextExtensions
{
    /// <summary>
    /// Lấy thông tin connection string (ẩn password nếu có)
    /// </summary>
    public static string GetConnectionInfo(this UniStyleDbContext context)
    {
        try
        {
            var connectionString = context.Database.GetConnectionString() ?? "N/A";
            // Ẩn password nếu có trong connection string
            if (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            {
                var parts = connectionString.Split(';');
                var safeParts = parts.Select(p => 
                    p.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) 
                        ? "Password=***" 
                        : p);
                connectionString = string.Join("; ", safeParts);
            }
            return connectionString;
        }
        catch
        {
            return "Không thể lấy thông tin connection string";
        }
    }

    /// <summary>
    /// Test kết nối database
    /// </summary>
    public static bool TestConnection(this UniStyleDbContext context)
    {
        try
        {
            return context.Database.CanConnect();
        }
        catch
        {
            return false;
        }
    }
}








