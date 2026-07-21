using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResaleValueTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentResaleValue",
                table: "Watches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResaleValueUpdatedAt",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResaleValueEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResaleValueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResaleValueEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResaleValueEntries_Watches_WatchId",
                        column: x => x.WatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResaleValueEntries_UserId",
                table: "ResaleValueEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResaleValueEntries_WatchId_RecordedAt",
                table: "ResaleValueEntries",
                columns: new[] { "WatchId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResaleValueEntries");

            migrationBuilder.DropColumn(
                name: "CurrentResaleValue",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "ResaleValueUpdatedAt",
                table: "Watches");
        }
    }
}
