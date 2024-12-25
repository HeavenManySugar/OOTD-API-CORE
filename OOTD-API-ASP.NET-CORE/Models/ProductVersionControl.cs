using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class ProductVersionControl
{
    public int Pvcid { get; set; }

    public int ProductId { get; set; }

    public int Version { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Product Product { get; set; } = null!;
}
