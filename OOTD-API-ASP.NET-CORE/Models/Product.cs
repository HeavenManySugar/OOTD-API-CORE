using System;
using System.Collections.Generic;

namespace OOTD_API_ASP.NET_CORE.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int StoreId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Enabled { get; set; }

    public int Quantity { get; set; }

    public virtual ICollection<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductKeyword> ProductKeywords { get; set; } = new List<ProductKeyword>();

    public virtual ICollection<ProductVersionControl> ProductVersionControls { get; set; } = new List<ProductVersionControl>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual Store Store { get; set; } = null!;
}
