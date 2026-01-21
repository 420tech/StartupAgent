using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");
        }
    }
}
