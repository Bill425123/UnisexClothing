using System;
using System.Collections.Generic;

namespace UnisexClothes.Models;

public partial class Favorite
{
    public int FavoriteId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
