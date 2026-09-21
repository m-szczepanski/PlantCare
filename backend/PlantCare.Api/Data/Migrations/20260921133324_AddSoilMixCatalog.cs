using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoilMixCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoilMixes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoilMixes");
        }
    }
}
