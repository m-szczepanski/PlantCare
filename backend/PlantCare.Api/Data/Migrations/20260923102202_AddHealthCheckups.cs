using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthCheckups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckupReminderSentAt",
                table: "Plants",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "Plants",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckupAt",
                table: "Plants",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlantHealthChecks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(nullable: false),
                    CheckedAt = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    Note = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantHealthChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantHealthChecks_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantHealthChecks_PlantId_CheckedAt",
                table: "PlantHealthChecks",
                columns: new[] { "PlantId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantHealthChecks");

            migrationBuilder.DropColumn(
                name: "CheckupReminderSentAt",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "LastCheckupAt",
                table: "Plants");
        }
    }
}
