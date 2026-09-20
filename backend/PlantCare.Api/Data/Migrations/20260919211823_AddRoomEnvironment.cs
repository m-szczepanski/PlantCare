using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Humidity",
                table: "Rooms",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LightExposure",
                table: "Rooms",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemperatureCelsius",
                table: "Rooms",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Humidity",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "LightExposure",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "TemperatureCelsius",
                table: "Rooms");
        }
    }
}
