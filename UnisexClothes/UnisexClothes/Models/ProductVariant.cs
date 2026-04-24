using System;
using System.Collections.Generic;

namespace UnisexClothes.Models;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public int? StockQuantity { get; set; }

    public decimal? AdditionalPrice { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Product Product { get; set; } = null!;
}
