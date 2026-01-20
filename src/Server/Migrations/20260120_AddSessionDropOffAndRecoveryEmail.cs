using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDropOffAndRecoveryEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create SessionDropOffs table
            migrationBuilder.CreateTable(
                name: "SessionDropOffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FounderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionDropOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionDropOffs_Founders_FounderId",
                        column: x => x.FounderId,
                        principalTable: "Founders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create RecoveryEmails table
            migrationBuilder.CreateTable(
                name: "RecoveryEmails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SessionDropOffId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    FounderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ResumeLink = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryEmails_Founders_FounderId",
                        column: x => x.FounderId,
                        principalTable: "Founders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for SessionDropOffs
            migrationBuilder.CreateIndex(
                name: "IX_SessionDropOffs_CreatedAt",
                table: "SessionDropOffs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionDropOffs_FounderId",
                table: "SessionDropOffs",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionDropOffs_FounderId_Status",
                table: "SessionDropOffs",
                columns: new[] { "FounderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionDropOffs_Status",
                table: "SessionDropOffs",
                column: "Status");

            // Create indexes for RecoveryEmails
            migrationBuilder.CreateIndex(
                name: "IX_RecoveryEmails_CreatedAt",
                table: "RecoveryEmails",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryEmails_FounderId",
                table: "RecoveryEmails",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryEmails_Status",
                table: "RecoveryEmails",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryEmails_Status_CreatedAt",
                table: "RecoveryEmails",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryEmails");

            migrationBuilder.DropTable(
                name: "SessionDropOffs");
        }
    }
}
