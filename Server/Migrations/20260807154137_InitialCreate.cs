using Microsoft.EntityFrameworkCore.Migrations;

namespace FarmerQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ToUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserIdA = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserIdB = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    GameKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HostUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActiveDriverUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BlockedUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Team = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    GameStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSessionInvitations",
                columns: table => new
                {
                    InvitationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvitedUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvitedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionInvitations", x => x.InvitationId);
                    table.ForeignKey(
                        name: "FK_GameSessionInvitations_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessionPlayers",
                columns: table => new
                {
                    GameSessionPlayerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsHost = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionPlayers", x => x.GameSessionPlayerId);
                    table.ForeignKey(
                        name: "FK_GameSessionPlayers_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TotalXp = table.Column<int>(type: "int", nullable: false),
                    ClimateActionXp = table.Column<int>(type: "int", nullable: false),
                    NatureConservationXp = table.Column<int>(type: "int", nullable: false),
                    EnergiManagementXp = table.Column<int>(type: "int", nullable: false),
                    AgriProductionXp = table.Column<int>(type: "int", nullable: false),
                    TechnologyXp = table.Column<int>(type: "int", nullable: false),
                    RoundsPlayed = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PlayerStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BadgeProgresses",
                columns: table => new
                {
                    BadgeProgressId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BadgeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Completions = table.Column<int>(type: "int", nullable: false),
                    BestCompletions = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeProgresses", x => x.BadgeProgressId);
                    table.ForeignKey(
                        name: "FK_BadgeProgresses_PlayerStats_UserId",
                        column: x => x.UserId,
                        principalTable: "PlayerStats",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeProgresses_UserId_BadgeKey",
                table: "BadgeProgresses",
                columns: new[] { "UserId", "BadgeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_FromUserId_Status",
                table: "FriendRequests",
                columns: new[] { "FromUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_FromUserId_ToUserId_Status",
                table: "FriendRequests",
                columns: new[] { "FromUserId", "ToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_ToUserId_Status",
                table: "FriendRequests",
                columns: new[] { "ToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserIdA",
                table: "Friendships",
                column: "UserIdA");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserIdA_UserIdB",
                table: "Friendships",
                columns: new[] { "UserIdA", "UserIdB" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserIdB",
                table: "Friendships",
                column: "UserIdB");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionInvitations_InvitedUserId_Status",
                table: "GameSessionInvitations",
                columns: new[] { "InvitedUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionInvitations_SessionId_InvitedUserId_Status",
                table: "GameSessionInvitations",
                columns: new[] { "SessionId", "InvitedUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionPlayers_SessionId_UserId",
                table: "GameSessionPlayers",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Code",
                table: "GameSessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Status",
                table: "GameSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedUserId",
                table: "UserBlocks",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerUserId_BlockedUserId",
                table: "UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeProgresses");

            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "GameSessionInvitations");

            migrationBuilder.DropTable(
                name: "GameSessionPlayers");

            migrationBuilder.DropTable(
                name: "UserBlocks");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
