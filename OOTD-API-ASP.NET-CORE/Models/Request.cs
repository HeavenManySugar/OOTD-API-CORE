using System;
using System.Collections.Generic;

namespace OOTD_API_ASP.NET_CORE.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int Uid { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Message { get; set; } = null!;

    public int StatusId { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual User UidNavigation { get; set; } = null!;
}
