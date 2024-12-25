using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int Uid { get; set; }

    public int? CouponId { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Status Status { get; set; } = null!;

    public virtual User UidNavigation { get; set; } = null!;
}
