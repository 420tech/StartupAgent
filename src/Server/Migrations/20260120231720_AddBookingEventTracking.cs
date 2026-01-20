using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingEventTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FounderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingEvents_Founders_FounderId",
                        column: x => x.FounderId,
                        principalTable: "Founders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_CorrelationId",
                table: "BookingEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_CreatedAt",
                table: "BookingEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_EventType",
                table: "BookingEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_FounderId",
                table: "BookingEvents",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_FounderId_CreatedAt",
                table: "BookingEvents",
                columns: new[] { "FounderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_Source",
                table: "BookingEvents",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingEvents");
        }
    }
}
