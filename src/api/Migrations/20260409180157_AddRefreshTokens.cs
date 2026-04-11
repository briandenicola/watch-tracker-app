using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use IF NOT EXISTS for all pre-existing tables so this migration
            // works on both fresh databases and existing deployments that were
            // created before migrations were tracked in git.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "AppSettings" (
                    "Key" TEXT NOT NULL CONSTRAINT "PK_AppSettings" PRIMARY KEY,
                    "Value" TEXT NOT NULL
                );
            """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
                    "Username" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "ProfileImage" TEXT NULL,
                    "Role" TEXT NOT NULL,
                    "FailedLoginAttempts" INTEGER NOT NULL,
                    "LockoutEnd" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL
                );
            """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "ApiKeys" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ApiKeys" PRIMARY KEY AUTOINCREMENT,
                    "UserId" INTEGER NOT NULL,
                    "Name" TEXT NOT NULL,
                    "KeyHash" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "LastUsedAt" TEXT NULL,
                    CONSTRAINT "FK_ApiKeys_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
            """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Watches" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Watches" PRIMARY KEY AUTOINCREMENT,
                    "Brand" TEXT NOT NULL,
                    "Model" TEXT NOT NULL,
                    "MovementType" TEXT NOT NULL,
                    "CaseSizeMm" REAL NULL,
                    "BandType" TEXT NULL,
                    "BandColor" TEXT NULL,
                    "PurchaseDate" TEXT NULL,
                    "PurchasePrice" decimal(18,2) NULL,
                    "Notes" TEXT NULL,
                    "AiAnalysis" TEXT NULL,
                    "LastWornDate" TEXT NULL,
                    "TimesWorn" INTEGER NOT NULL,
                    "CrystalType" TEXT NULL,
                    "CaseShape" TEXT NULL,
                    "CrownType" TEXT NULL,
                    "CalendarType" TEXT NULL,
                    "CountryOfOrigin" TEXT NULL,
                    "WaterResistance" TEXT NULL,
                    "LugWidthMm" REAL NULL,
                    "DialColor" TEXT NULL,
                    "BezelType" TEXT NULL,
                    "PowerReserveHours" INTEGER NULL,
                    "SerialNumber" TEXT NULL,
                    "BatteryType" TEXT NULL,
                    "LinkUrl" TEXT NULL,
                    "LinkText" TEXT NULL,
                    "IsWishList" INTEGER NOT NULL,
                    "UserId" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Watches_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
            """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "WatchImages" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_WatchImages" PRIMARY KEY AUTOINCREMENT,
                    "WatchId" INTEGER NOT NULL,
                    "FileName" TEXT NOT NULL,
                    "ContentType" TEXT NOT NULL,
                    "SortOrder" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_WatchImages_Watches_WatchId" FOREIGN KEY ("WatchId") REFERENCES "Watches" ("Id") ON DELETE CASCADE
                );
            """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "WearLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_WearLogs" PRIMARY KEY AUTOINCREMENT,
                    "WatchId" INTEGER NOT NULL,
                    "UserId" INTEGER NOT NULL,
                    "WornDate" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_WearLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_WearLogs_Watches_WatchId" FOREIGN KEY ("WatchId") REFERENCES "Watches" ("Id") ON DELETE CASCADE
                );
            """);

            // RefreshTokens is the only truly new table
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_ApiKeys_KeyHash" ON "ApiKeys" ("KeyHash");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_ApiKeys_UserId" ON "ApiKeys" ("UserId");""");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Watches_UserId" ON "Watches" ("UserId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_WatchImages_WatchId" ON "WatchImages" ("WatchId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_WearLogs_UserId" ON "WearLogs" ("UserId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_WearLogs_WatchId" ON "WearLogs" ("WatchId");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "WatchImages");

            migrationBuilder.DropTable(
                name: "WearLogs");

            migrationBuilder.DropTable(
                name: "Watches");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
