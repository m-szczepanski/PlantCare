using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyDigest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.AddColumn<bool>(
                name: "NotifyEnabled",
                table: "Plants",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "NotificationDigests",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SentAt = table.Column<DateTime>( nullable: false),
                    PlantCount = table.Column<int>( nullable: false),
                    OverdueCount = table.Column<int>( nullable: false),
                    Priority = table.Column<int>( nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDigests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDigests_SentAt",
                table: "NotificationDigests",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDigests");

            migrationBuilder.DropColumn(
                name: "NotifyEnabled",
                table: "Plants");

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

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_PlantId_Type_SentAt",
                table: "NotificationLogs",
                columns: new[] { "PlantId", "Type", "SentAt" });
        }
    }
}
