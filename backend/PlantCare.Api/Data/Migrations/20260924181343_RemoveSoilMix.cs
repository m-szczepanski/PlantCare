using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoilMix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoilMixes");

            migrationBuilder.DropColumn(
                name: "SoilMix",
                table: "Plants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SoilMix",
                table: "Plants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoilMixes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoilMixes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoilMixes_Name",
                table: "SoilMixes",
                column: "Name",
                unique: true);
        }
    }
}
