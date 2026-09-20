using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaterDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmountMilliliters",
                table: "CareTaskLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "CareTaskLogs",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountMilliliters",
                table: "CareTaskLogs");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "CareTaskLogs");
        }
    }
}
