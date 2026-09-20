using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Plants",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>( nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>( nullable: false),
                    Orientation = table.Column<string>( nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_RoomId",
                table: "Plants",
                column: "RoomId");

            // Move the free-text Location values into Room rows before dropping the column,
            // so no existing plant data is lost by the schema change.
            migrationBuilder.Sql(
                """
                INSERT INTO "Rooms" ("Name")
                SELECT DISTINCT TRIM("Location") FROM "Plants"
                WHERE "Location" IS NOT NULL AND TRIM("Location") <> ''
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Plants" SET "RoomId" = (
                    SELECT "Rooms"."Id" FROM "Rooms" WHERE "Rooms"."Name" = TRIM("Plants"."Location")
                )
                WHERE "Location" IS NOT NULL AND TRIM("Location") <> ''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Rooms_RoomId",
                table: "Plants",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Plants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Rooms_RoomId",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_Plants_RoomId",
                table: "Plants");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Plants",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Plants" SET "Location" = COALESCE(
                    (SELECT "Rooms"."Name" FROM "Rooms" WHERE "Rooms"."Id" = "Plants"."RoomId"), '')
                """);

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Plants");
        }
    }
}
