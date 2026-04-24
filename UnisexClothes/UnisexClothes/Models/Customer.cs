using System;
using System.Collections.Generic;

namespace UnisexClothes.Models
{
    public partial class Customer
    {
        public int CustomerId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        // Removed Orders navigation - Orders table uses UserId, not CustomerId
    }
}
