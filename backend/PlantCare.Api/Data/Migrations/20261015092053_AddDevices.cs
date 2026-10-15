using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "Plants",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: false),
                    CommandStyle = table.Column<string>(nullable: false),
                    CommandTopic = table.Column<string>(nullable: true),
                    StateTopic = table.Column<string>(nullable: true),
                    LastMoisturePct = table.Column<int>(nullable: true),
                    WaterLevel = table.Column<string>(nullable: true),
                    LastSeenAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_DeviceId",
                table: "Plants",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Devices_DeviceId",
                table: "Plants",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Devices_DeviceId",
                table: "Plants");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Plants_DeviceId",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Plants");
        }
    }
}
