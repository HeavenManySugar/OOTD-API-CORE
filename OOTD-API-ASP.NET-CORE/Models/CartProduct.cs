using System;
using System.Collections.Generic;

namespace OOTD_API_ASP.NET_CORE.Models;

public partial class CartProduct
{
    public int CartId { get; set; }

    public int Uid { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User UidNavigation { get; set; } = null!;
}
