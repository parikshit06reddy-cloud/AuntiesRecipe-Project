using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuntiesRecipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminBusinessBrandingAndCategoryCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "BusinessProfiles",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroImagePath",
                table: "BusinessProfiles",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MapEmbedUrl",
                table: "BusinessProfiles",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "BusinessProfiles",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "HeroImagePath",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "MapEmbedUrl",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "BusinessProfiles");
        }
    }
}
