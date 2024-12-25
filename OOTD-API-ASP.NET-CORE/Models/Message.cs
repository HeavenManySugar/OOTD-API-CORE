﻿using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string Message1 { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
