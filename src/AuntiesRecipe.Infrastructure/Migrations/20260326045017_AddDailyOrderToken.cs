using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuntiesRecipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyOrderToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyTokenNumber",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenDateUtc",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TokenDateUtc_DailyTokenNumber",
                table: "Orders",
                columns: new[] { "TokenDateUtc", "DailyTokenNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TokenDateUtc_DailyTokenNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DailyTokenNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TokenDateUtc",
                table: "Orders");
        }
    }
}
