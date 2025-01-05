using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class Rating
{
    public int RatingId { get; set; }

    public int ProductId { get; set; }

    public int Uid { get; set; }

    public double Rating1 { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Description { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User UidNavigation { get; set; } = null!;
}
