using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiagnosisChecklist",
                table: "PlantProfiles",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosisChecklist",
                table: "PlantProfiles");
        }
    }
}
