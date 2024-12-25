using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class Store
{
    public int StoreId { get; set; }

    public int OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool Enabled { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
