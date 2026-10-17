using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations;

public partial class AddWatchPatrolConnection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WatchPatrolConnections",
            columns: table => new
            {
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                CredentialsProtected = table.Column<string>(type: "TEXT", nullable: true),
                PendingStateHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                PendingAuthorizationProtected = table.Column<string>(type: "TEXT", nullable: true),
                PendingExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WatchPatrolConnections", c => c.UserId);
                table.ForeignKey(
                    name: "FK_WatchPatrolConnections_Users_UserId",
                    column: c => c.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_WatchPatrolConnections_PendingStateHash",
            table: "WatchPatrolConnections",
            column: "PendingStateHash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "WatchPatrolConnections");
}
