using System;
using System.Collections.Generic;

namespace Api.Models;

public partial class Pcrjson
{
    public long PcrId { get; set; }

    public string? JsonData { get; set; }

    public virtual Pcr Pcr { get; set; } = null!;
}
