using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantProfiles",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommonName = table.Column<string>( nullable: false),
                    ScientificName = table.Column<string>( nullable: true),
                    DefaultWateringIntervalDays = table.Column<int>( nullable: false),
                    LightRequirement = table.Column<string>( nullable: false),
                    HumidityNotes = table.Column<string>( nullable: false),
                    CareTips = table.Column<string>( nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantProfileId = table.Column<int>( nullable: true),
                    NickName = table.Column<string>( nullable: false),
                    Location = table.Column<string>( nullable: false),
                    PhotoUrl = table.Column<string>( nullable: true),
                    AcquiredDate = table.Column<DateTime>( nullable: false),
                    CustomWateringIntervalDays = table.Column<int>( nullable: true),
                    LastWateredAt = table.Column<DateTime>( nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_PlantProfiles_PlantProfileId",
                        column: x => x.PlantProfileId,
                        principalTable: "PlantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>( nullable: false),
                    SentAt = table.Column<DateTime>( nullable: false),
                    Type = table.Column<string>( nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WateringLogs",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>( nullable: false),
                    WateredAt = table.Column<DateTime>( nullable: false),
                    Note = table.Column<string>( nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WateringLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WateringLogs_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_PlantId_Type_SentAt",
                table: "NotificationLogs",
                columns: new[] { "PlantId", "Type", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantProfiles_CommonName",
                table: "PlantProfiles",
                column: "CommonName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantProfileId",
                table: "Plants",
                column: "PlantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WateringLogs_PlantId_WateredAt",
                table: "WateringLogs",
                columns: new[] { "PlantId", "WateredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "WateringLogs");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "PlantProfiles");
        }
    }
}
