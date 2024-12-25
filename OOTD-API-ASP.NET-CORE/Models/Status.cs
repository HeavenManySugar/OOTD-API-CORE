using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class Status
{
    public int StatusId { get; set; }

    public string Status1 { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
