using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckAnalysisNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create DeckAnalysisNotifications table
            migrationBuilder.CreateTable(
                name: "DeckAnalysisNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    DeckAnalysisId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    FounderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckAnalysisNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckAnalysisNotifications_Founders_FounderId",
                        column: x => x.FounderId,
                        principalTable: "Founders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for DeckAnalysisNotifications
            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_CorrelationId",
                table: "DeckAnalysisNotifications",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_CreatedAt",
                table: "DeckAnalysisNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_FounderId",
                table: "DeckAnalysisNotifications",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_NotificationType",
                table: "DeckAnalysisNotifications",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_Status",
                table: "DeckAnalysisNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeckAnalysisNotifications_Status_CreatedAt",
                table: "DeckAnalysisNotifications",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeckAnalysisNotifications");
        }
    }
}
