using System;
using System.Collections.Generic;

namespace UnisexClothes.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public decimal SubTotal { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? OrderStatus { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? DeliveredDate { get; set; }

    public string? Notes { get; set; }

    public int? CouponId { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User? User { get; set; }

    public virtual Coupon? Coupon { get; set; }
}
