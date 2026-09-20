using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCareTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    IntervalDays = table.Column<int>(type: "INTEGER", nullable: true),
                    LastDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareTasks_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareTaskLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CareTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareTaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareTaskLogs_CareTasks_CareTaskId",
                        column: x => x.CareTaskId,
                        principalTable: "CareTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskLogs_CareTaskId_DoneAt",
                table: "CareTaskLogs",
                columns: new[] { "CareTaskId", "DoneAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareTasks_PlantId_Type",
                table: "CareTasks",
                columns: new[] { "PlantId", "Type" },
                unique: true);

            // Carry every plant's watering schedule (interval override + last watered) into a typed task...
            migrationBuilder.Sql(
                """
                INSERT INTO CareTasks (PlantId, Type, IntervalDays, LastDoneAt)
                SELECT Id, 'Watering', CustomWateringIntervalDays, LastWateredAt FROM Plants
                """);

            // ...and move the whole watering history into the generic task log before dropping the old tables/columns.
            migrationBuilder.Sql(
                """
                INSERT INTO CareTaskLogs (CareTaskId, DoneAt, Note)
                SELECT ct.Id, w.WateredAt, w.Note
                FROM WateringLogs w
                JOIN CareTasks ct ON ct.PlantId = w.PlantId AND ct.Type = 'Watering'
                """);

            migrationBuilder.DropTable(
                name: "WateringLogs");

            migrationBuilder.DropColumn(
                name: "CustomWateringIntervalDays",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "LastWateredAt",
                table: "Plants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomWateringIntervalDays",
                table: "Plants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWateredAt",
                table: "Plants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Plants SET
                    CustomWateringIntervalDays = (SELECT ct.IntervalDays FROM CareTasks ct WHERE ct.PlantId = Plants.Id AND ct.Type = 'Watering'),
                    LastWateredAt = (SELECT ct.LastDoneAt FROM CareTasks ct WHERE ct.PlantId = Plants.Id AND ct.Type = 'Watering')
                """);

            migrationBuilder.CreateTable(
                name: "WateringLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    WateredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.Sql(
                """
                INSERT INTO WateringLogs (PlantId, WateredAt, Note)
                SELECT ct.PlantId, l.DoneAt, l.Note
                FROM CareTaskLogs l
                JOIN CareTasks ct ON ct.Id = l.CareTaskId
                WHERE ct.Type = 'Watering'
                """);

            migrationBuilder.DropTable(
                name: "CareTaskLogs");

            migrationBuilder.DropTable(
                name: "CareTasks");

            migrationBuilder.CreateIndex(
                name: "IX_WateringLogs_PlantId_WateredAt",
                table: "WateringLogs",
                columns: new[] { "PlantId", "WateredAt" });
        }
    }
}
