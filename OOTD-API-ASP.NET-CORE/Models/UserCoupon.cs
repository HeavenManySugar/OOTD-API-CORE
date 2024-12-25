using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class UserCoupon
{
    public int UserCouponId { get; set; }

    public int Uid { get; set; }

    public int CouponId { get; set; }

    public int Quantity { get; set; }

    public virtual Coupon Coupon { get; set; } = null!;

    public virtual User UidNavigation { get; set; } = null!;
}
