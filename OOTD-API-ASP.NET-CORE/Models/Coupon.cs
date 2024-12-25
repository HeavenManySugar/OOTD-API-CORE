using System;
using System.Collections.Generic;

namespace OOTD_API_ASP.NET_CORE.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Discount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpireDate { get; set; }

    public bool Enabled { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
}
