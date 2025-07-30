using System;
using System.Collections.Generic;

namespace EpcrExportConsoleApp.Models;

public partial class Pcr
{
    public long Id { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public string? ObjectId { get; set; }

    public int? TenantId { get; set; }

    public string? IncidentDate { get; set; }

    public virtual Pcrjson? Pcrjson { get; set; }
}
