using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int Pvcid { get; set; }

    public int Quantity { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductVersionControl Pvc { get; set; } = null!;
}
