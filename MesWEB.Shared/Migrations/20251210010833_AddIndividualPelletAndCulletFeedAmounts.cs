#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace MesWEB.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddIndividualPelletAndCulletFeedAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cullet1Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cullet2Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cullet3Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pellet1Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pellet2Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pellet3Feed",
                table: "GrowthNotes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cullet1Feed",
                table: "GrowthNotes");

            migrationBuilder.DropColumn(
                name: "Cullet2Feed",
                table: "GrowthNotes");

            migrationBuilder.DropColumn(
                name: "Cullet3Feed",
                table: "GrowthNotes");

            migrationBuilder.DropColumn(
                name: "Pellet1Feed",
                table: "GrowthNotes");

            migrationBuilder.DropColumn(
                name: "Pellet2Feed",
                table: "GrowthNotes");

            migrationBuilder.DropColumn(
                name: "Pellet3Feed",
                table: "GrowthNotes");
        }
    }
}
