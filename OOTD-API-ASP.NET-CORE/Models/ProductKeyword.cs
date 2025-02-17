﻿using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class ProductKeyword
{
    public int ProduckKeywordId { get; set; }

    public int ProductId { get; set; }

    public string Keyword { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
