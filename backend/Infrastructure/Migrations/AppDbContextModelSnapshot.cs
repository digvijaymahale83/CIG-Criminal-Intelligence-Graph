using System;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Domain.Entities.AuditLog", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("Action").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("ActorId").HasColumnType("text");
            b.Property<string>("ActorName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("IpAddress").HasColumnType("text");
            b.Property<string>("MetadataJson").HasColumnType("text");
            b.Property<string>("ResourceId").HasColumnType("text");
            b.Property<string>("ResourceType").HasMaxLength(100).HasColumnType("character varying(100)");
            b.HasKey("Id");
            b.HasIndex("Action");
            b.HasIndex("CreatedAtUtc");
            b.ToTable("AuditLogs");
        });

        modelBuilder.Entity("Domain.Entities.Case", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("CaseNumber").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Category").IsRequired().HasColumnType("text");
            b.Property<string>("Classification").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("CreatedBy").IsRequired().HasColumnType("text");
            b.Property<string>("Description").IsRequired().HasColumnType("text");
            b.Property<string>("District").IsRequired().HasColumnType("text");
            b.Property<int>("EntityCount").HasColumnType("integer");
            b.Property<int>("EvidenceCount").HasColumnType("integer");
            b.Property<string>("FirNumber").IsRequired().HasColumnType("text");
            b.Property<string>("Jurisdiction").IsRequired().HasColumnType("text");
            b.Property<string>("LeadOfficerId").HasColumnType("text");
            b.Property<string>("LeadOfficerName").HasColumnType("text");
            b.Property<string>("PoliceStation").IsRequired().HasColumnType("text");
            b.Property<string>("Priority").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("CaseNumber").IsUnique();
            b.ToTable("Cases");
        });

        modelBuilder.Entity("Domain.Entities.EntityItem", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("AccountNumber").HasColumnType("text");
            b.Property<string>("BankName").HasColumnType("text");
            b.Property<string>("CanonicalName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("CaseId").HasColumnType("text");
            b.Property<double>("Confidence").HasColumnType("double precision");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Description").IsRequired().HasColumnType("text");
            b.Property<string>("District").IsRequired().HasColumnType("text");
            b.Property<string>("Location").IsRequired().HasColumnType("text");
            b.Property<string>("NormalizedValue").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("PhoneNumber").HasColumnType("text");
            b.Property<string>("RiskLevel").HasColumnType("text");
            b.Property<string>("Type").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("VehicleNumber").HasColumnType("text");
            b.Property<string>("VerificationStatus").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.HasKey("Id");
            b.HasIndex("CanonicalName");
            b.HasIndex("CaseId");
            b.HasIndex("Type");
            b.ToTable("Entities");
        });

        modelBuilder.Entity("Domain.Entities.Evidence", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("CaseId").HasColumnType("text");
            b.Property<string>("Clearance").IsRequired().HasColumnType("text");
            b.Property<string>("Description").IsRequired().HasColumnType("text");
            b.Property<string>("FileName").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<long>("FileSize").HasColumnType("bigint");
            b.Property<string>("MimeType").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            b.Property<string>("ProcessingStatus").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Sha256Hash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<string>("StoragePath").IsRequired().HasColumnType("text");
            b.Property<DateTime>("UploadedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("UploadedById").HasColumnType("text");
            b.Property<string>("UploadedByName").IsRequired().HasColumnType("text");
            b.HasKey("Id");
            b.HasIndex("CaseId");
            b.HasIndex("Sha256Hash");
            b.ToTable("EvidenceItems");
        });

        modelBuilder.Entity("Domain.Entities.Relationship", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("CaseId").HasColumnType("text");
            b.Property<double>("Confidence").HasColumnType("double precision");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("SourceEntityId").IsRequired().HasColumnType("text");
            b.Property<string>("SourceEvidenceId").HasColumnType("text");
            b.Property<string>("TargetEntityId").IsRequired().HasColumnType("text");
            b.Property<string>("Type").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime?>("ValidFrom").HasColumnType("timestamp with time zone");
            b.Property<DateTime?>("ValidTo").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("SourceEntityId", "TargetEntityId");
            b.ToTable("Relationships");
        });

        modelBuilder.Entity("Domain.Entities.User", b =>
        {
            b.Property<string>("Id").HasColumnType("text");
            b.Property<string>("Agency").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("BadgeNumber").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Email").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("FullName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("PasswordHash").IsRequired().HasColumnType("text");
            b.Property<string>("Rank").IsRequired().HasColumnType("text");
            b.Property<string>("Role").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Unit").IsRequired().HasColumnType("text");
            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("Users");
        });

        modelBuilder.Entity("Domain.Entities.EntityItem", b =>
        {
            b.HasOne("Domain.Entities.Case", "Case")
                .WithMany("Entities")
                .HasForeignKey("CaseId")
                .OnDelete(DeleteBehavior.SetNull);
            b.Navigation("Case");
        });

        modelBuilder.Entity("Domain.Entities.Evidence", b =>
        {
            b.HasOne("Domain.Entities.Case", "Case")
                .WithMany("EvidenceItems")
                .HasForeignKey("CaseId")
                .OnDelete(DeleteBehavior.SetNull);
            b.Navigation("Case");
        });

        modelBuilder.Entity("Domain.Entities.Case", b =>
        {
            b.Navigation("Entities");
            b.Navigation("EvidenceItems");
        });
    }
}
