using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PotSizeCm",
                table: "Plants",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropagatedFrom",
                table: "Plants",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoilMix",
                table: "Plants",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotSizeCm",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "PropagatedFrom",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "SoilMix",
                table: "Plants");
        }
    }
}
