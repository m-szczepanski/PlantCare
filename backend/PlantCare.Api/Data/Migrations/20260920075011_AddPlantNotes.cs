using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantNotes",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>( nullable: false),
                    CreatedAt = table.Column<DateTime>( nullable: false),
                    Text = table.Column<string>( nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantNotes_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantNotes_PlantId_CreatedAt",
                table: "PlantNotes",
                columns: new[] { "PlantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantNotes");
        }
    }
}
