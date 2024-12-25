using System;
using System.Collections.Generic;

namespace OOTD_API_ASP.NET_CORE.Models;

public partial class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Url { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
