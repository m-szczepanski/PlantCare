using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoilMix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fold the old soil-mix values into the soil-type category before the
            // drop so plants keep a functional substrate (mirrors SoilTypes.MixToType).
            migrationBuilder.Sql("""
                UPDATE Plants
                SET SoilType = CASE SoilMix
                    WHEN 'All-purpose potting mix' THEN 'AllPurpose'
                    WHEN 'Worm casting boost' THEN 'AllPurpose'
                    WHEN 'Leaf mold & loam' THEN 'AllPurpose'
                    WHEN 'Cactus & succulent mix' THEN 'CactusMix'
                    WHEN 'Pumice-heavy inorganic mix' THEN 'CactusMix'
                    WHEN 'Aroid chunky blend' THEN 'ChunkyBark'
                    WHEN 'Orchid bark mix' THEN 'ChunkyBark'
                    WHEN 'Peat & perlite mix' THEN 'PeatCoco'
                    WHEN 'Coco coir & perlite blend' THEN 'PeatCoco'
                    WHEN 'Sphagnum moss' THEN 'PeatCoco'
                    WHEN 'Semi-hydro LECA' THEN 'SemiHydro'
                    WHEN 'Self-watering pot blend' THEN 'SelfWatering'
                    ELSE SoilType
                END
                WHERE SoilType IS NULL AND SoilMix IS NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "SoilMixes");

            migrationBuilder.DropColumn(
                name: "SoilMix",
                table: "Plants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SoilMix",
                table: "Plants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoilMixes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
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
    }
}
