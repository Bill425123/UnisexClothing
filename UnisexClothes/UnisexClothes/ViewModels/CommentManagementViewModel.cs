using System.Collections.Generic;

namespace UnisexClothes.ViewModels;

public class CommentProductSummaryViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int TotalComments { get; set; }
    public int ApprovedComments { get; set; }
    public int PendingComments { get; set; }
    public double? AverageRating { get; set; }
}

public class CommentManagementViewModel
{
    public string? SearchProduct { get; set; }
    public int? CategoryId { get; set; }
    public List<CommentProductSummaryViewModel> Products { get; set; } = new();
    public List<Models.Category> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalProducts { get; set; }
    public int TotalComments { get; set; }
    public int TotalApproved { get; set; }
    public int TotalPending { get; set; }
}

