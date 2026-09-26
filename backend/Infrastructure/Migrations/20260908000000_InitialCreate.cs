using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Users
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                BadgeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Agency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Rank = table.Column<string>(type: "text", nullable: false),
                Unit = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        // 2. Cases
        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                CaseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                Jurisdiction = table.Column<string>(type: "text", nullable: false),
                District = table.Column<string>(type: "text", nullable: false),
                FirNumber = table.Column<string>(type: "text", nullable: false),
                PoliceStation = table.Column<string>(type: "text", nullable: false),
                LeadOfficerId = table.Column<string>(type: "text", nullable: true),
                LeadOfficerName = table.Column<string>(type: "text", nullable: true),
                EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                EntityCount = table.Column<int>(type: "integer", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => x.Id);
            });

        // 3. EvidenceItems
        migrationBuilder.CreateTable(
            name: "EvidenceItems",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                CaseId = table.Column<string>(type: "text", nullable: true),
                FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                StoragePath = table.Column<string>(type: "text", nullable: false),
                Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                UploadedById = table.Column<string>(type: "text", nullable: true),
                UploadedByName = table.Column<string>(type: "text", nullable: false),
                UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Clearance = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EvidenceItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_EvidenceItems_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // 4. AuditLogs
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                ActorId = table.Column<string>(type: "text", nullable: true),
                ActorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ResourceId = table.Column<string>(type: "text", nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                IpAddress = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        // 5. Entities
        migrationBuilder.CreateTable(
            name: "Entities",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                CaseId = table.Column<string>(type: "text", nullable: true),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CanonicalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                NormalizedValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Confidence = table.Column<double>(type: "double precision", nullable: false),
                VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RiskLevel = table.Column<string>(type: "text", nullable: true),
                Description = table.Column<string>(type: "text", nullable: false),
                Location = table.Column<string>(type: "text", nullable: false),
                District = table.Column<string>(type: "text", nullable: false),
                PhoneNumber = table.Column<string>(type: "text", nullable: true),
                VehicleNumber = table.Column<string>(type: "text", nullable: true),
                AccountNumber = table.Column<string>(type: "text", nullable: true),
                BankName = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Entities", x => x.Id);
                table.ForeignKey(
                    name: "FK_Entities_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // 6. Relationships
        migrationBuilder.CreateTable(
            name: "Relationships",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                CaseId = table.Column<string>(type: "text", nullable: true),
                SourceEntityId = table.Column<string>(type: "text", nullable: false),
                TargetEntityId = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SourceEvidenceId = table.Column<string>(type: "text", nullable: true),
                Confidence = table.Column<double>(type: "double precision", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Relationships", x => x.Id);
            });

        // Indexes
        migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Cases_CaseNumber", table: "Cases", column: "CaseNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_EvidenceItems_CaseId", table: "EvidenceItems", column: "CaseId");
        migrationBuilder.CreateIndex(name: "IX_EvidenceItems_Sha256Hash", table: "EvidenceItems", column: "Sha256Hash");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_Action", table: "AuditLogs", column: "Action");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_CreatedAtUtc", table: "AuditLogs", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_Entities_CanonicalName", table: "Entities", column: "CanonicalName");
        migrationBuilder.CreateIndex(name: "IX_Entities_CaseId", table: "Entities", column: "CaseId");
        migrationBuilder.CreateIndex(name: "IX_Entities_Type", table: "Entities", column: "Type");
        migrationBuilder.CreateIndex(name: "IX_Relationships_SourceEntityId_TargetEntityId", table: "Relationships", columns: new[] { "SourceEntityId", "TargetEntityId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Relationships");
        migrationBuilder.DropTable(name: "Entities");
        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropTable(name: "EvidenceItems");
        migrationBuilder.DropTable(name: "Cases");
        migrationBuilder.DropTable(name: "Users");
    }
}
