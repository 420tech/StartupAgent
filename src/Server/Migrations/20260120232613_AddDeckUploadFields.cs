using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "DeckAnalyses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "DeckAnalyses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DeckAnalyses",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "DeckAnalyses");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "DeckAnalyses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DeckAnalyses");
        }
    }
}
