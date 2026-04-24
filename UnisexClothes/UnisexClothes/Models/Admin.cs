using System;

namespace UnisexClothes.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string Role { get; set; } = "admin";

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
}








