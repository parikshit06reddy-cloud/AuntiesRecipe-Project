using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuntiesRecipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAdminQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupName_PickupPhone",
                table: "Orders",
                columns: new[] { "PickupName", "PickupPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PickupName_PickupPhone",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");
        }
    }
}
