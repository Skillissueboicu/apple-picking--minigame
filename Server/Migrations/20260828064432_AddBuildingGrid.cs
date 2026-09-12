using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmerQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    BuildingType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingPlacements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingPlacements_UserId_X_Y",
                table: "BuildingPlacements",
                columns: new[] { "UserId", "X", "Y" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingPlacements");
        }
    }
}
