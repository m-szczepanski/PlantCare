using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantProfileTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    CommonName = table.Column<string>(type: "TEXT", nullable: true),
                    HumidityNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CareTips = table.Column<string>(type: "TEXT", nullable: true),
                    DiagnosisChecklist = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantProfileTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantProfileTranslations_PlantProfiles_PlantProfileId",
                        column: x => x.PlantProfileId,
                        principalTable: "PlantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantProfileTranslations_PlantProfileId_Language",
                table: "PlantProfileTranslations",
                columns: new[] { "PlantProfileId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantProfileTranslations");
        }
    }
}
