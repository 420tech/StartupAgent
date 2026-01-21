using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Server.Migrations;

/// <summary>
/// Migration to create email template tables for transactional template system.
/// Supports versioning, A/B testing, and multi-language templates.
/// </summary>
public partial class AddEmailTemplateSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EmailTemplates table
        migrationBuilder.CreateTable(
            name: "EmailTemplates",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TemplateCode = table.Column<string>(type: "nvarchar(255)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                Language = table.Column<int>(type: "int", nullable: false),
                Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Variables = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                ABTestVariant = table.Column<string>(type: "nvarchar(50)", nullable: true),
                ABTestId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                CreatedBy = table.Column<string>(type: "nvarchar(255)", nullable: false, defaultValue: "system"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedBy = table.Column<string>(type: "nvarchar(255)", nullable: false, defaultValue: "system"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ChangeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailTemplates", x => x.Id);
            });

        // EmailTemplateVersions table
        migrationBuilder.CreateTable(
            name: "EmailTemplateVersions",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TemplateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(255)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                ChangeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailTemplateVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailTemplateVersions_EmailTemplates_TemplateId",
                    column: x => x.TemplateId,
                    principalTable: "EmailTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // EmailTemplateABTests table
        migrationBuilder.CreateTable(
            name: "EmailTemplateABTests",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TemplateCode = table.Column<string>(type: "nvarchar(255)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ControlVariant = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "control"),
                TestVariants = table.Column<string>(type: "nvarchar(max)", nullable: false),
                WinnerVariant = table.Column<string>(type: "nvarchar(50)", nullable: true),
                ControlSentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                VariantSentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedBy = table.Column<string>(type: "nvarchar(255)", nullable: false, defaultValue: "system"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                ConcludedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailTemplateABTests", x => x.Id);
            });

        // Create indexes for performance
        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplates_TemplateCode_Language",
            table: "EmailTemplates",
            columns: new[] { "TemplateCode", "Language" },
            unique: false);

        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplates_Type_IsActive",
            table: "EmailTemplates",
            columns: new[] { "Type", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplates_ABTestId",
            table: "EmailTemplates",
            column: "ABTestId");

        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplateVersions_TemplateId_Version",
            table: "EmailTemplateVersions",
            columns: new[] { "TemplateId", "Version" },
            unique: false);

        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplateABTests_TemplateCode",
            table: "EmailTemplateABTests",
            column: "TemplateCode");

        migrationBuilder.CreateIndex(
            name: "IX_EmailTemplateABTests_IsActive",
            table: "EmailTemplateABTests",
            column: "IsActive");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmailTemplateABTests");

        migrationBuilder.DropTable(
            name: "EmailTemplateVersions");

        migrationBuilder.DropTable(
            name: "EmailTemplates");
    }
}
