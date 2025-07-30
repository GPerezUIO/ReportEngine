using System;
using System.Collections.Generic;

namespace EpcrExportConsoleApp.Models;

public partial class DropdownOption
{
    public long Id { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public long? ParentId { get; set; }

    public string? Selector { get; set; }

    public string? Guid { get; set; }

    public string? Key { get; set; }

    public string? Value { get; set; }

    public int? TenantId { get; set; }

    public bool IsActive { get; set; }

    public string? Collection { get; set; }

    public string? Grouping { get; set; }

    public int Operation { get; set; }

    public string? Tags { get; set; }

    public int Order { get; set; }

    public virtual ICollection<DropdownOption> InverseParent { get; set; } = new List<DropdownOption>();

    public virtual DropdownOption? Parent { get; set; }
}
