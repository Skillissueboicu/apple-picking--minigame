using Microsoft.EntityFrameworkCore.Migrations;

namespace FarmerQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmSaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FarmSaveId",
                table: "GameSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FarmSaves",
                columns: table => new
                {
                    SaveId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateVersion = table.Column<int>(type: "int", nullable: false),
                    Money = table.Column<int>(type: "int", nullable: false),
                    Co2 = table.Column<float>(type: "real", nullable: false),
                    Debt = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmSaves", x => x.SaveId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_FarmSaveId",
                table: "GameSessions",
                column: "FarmSaveId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmSaves_OwnerUserId",
                table: "FarmSaves",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmSaves_OwnerUserId_LastPlayedAt",
                table: "FarmSaves",
                columns: new[] { "OwnerUserId", "LastPlayedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_FarmSaves_FarmSaveId",
                table: "GameSessions",
                column: "FarmSaveId",
                principalTable: "FarmSaves",
                principalColumn: "SaveId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_FarmSaves_FarmSaveId",
                table: "GameSessions");

            migrationBuilder.DropTable(
                name: "FarmSaves");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_FarmSaveId",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "FarmSaveId",
                table: "GameSessions");
        }
    }
}
