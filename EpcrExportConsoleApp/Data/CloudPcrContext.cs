using System;
using System.Collections.Generic;
using EpcrExportConsoleApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EpcrExportConsoleApp.Data;

public partial class CloudPcrContext : DbContext
{
    public CloudPcrContext()
    {
    }

    public CloudPcrContext(DbContextOptions<CloudPcrContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DropdownOption> DropdownOptions { get; set; }

    public virtual DbSet<Pcr> Pcrs { get; set; }

    public virtual DbSet<Pcrjson> Pcrjsons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("User ID=sqladmin;Password=1qaz!QAZ;Initial Catalog=cloudpcr-v4-dev;Server=tcp:cloudpcr-v4.database.windows.net,1433;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DropdownOption>(entity =>
        {
            entity.HasIndex(e => e.ParentId, "IX_DropdownOptions_ParentId");

            entity.HasIndex(e => new { e.IsDeleted, e.TenantId }, "nci_msft_1_DropdownOptions_08670A4A382408D619DA66FB564D85EF");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);
        });

        modelBuilder.Entity<Pcr>(entity =>
        {
            entity.ToTable("PCRs");

            entity.HasIndex(e => e.IncidentDate, "PCRXIncidentDate");

            entity.HasIndex(e => new { e.IncidentDate, e.CreationTime }, "PCRXIncidentDateAndCreationTime");

            entity.HasIndex(e => e.TenantId, "PCRXTenant");

            entity.HasIndex(e => new { e.IsDeleted, e.TenantId }, "PCRs_IsDeleted_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.IsDeleted }, "nci_msft_1_PCRs_1A2329494E0B43AC5920208FACA081BB");

            entity.HasIndex(e => new { e.CreatorUserId, e.IsDeleted }, "nci_msft_1_PCRs_38FDEAE6332B72D37C80AA7AB25AADEE");

            entity.HasIndex(e => new { e.TenantId, e.IsDeleted }, "nci_wi_PCRs_182F930BC394635A32D8ADC0351CB9B3");

            entity.Property(e => e.IncidentDate)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pcrjson>(entity =>
        {
            entity.HasKey(e => e.PcrId);

            entity.ToTable("PCRJson");

            entity.Property(e => e.PcrId).ValueGeneratedNever();

            entity.HasOne(d => d.Pcr).WithOne(p => p.Pcrjson).HasForeignKey<Pcrjson>(d => d.PcrId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
