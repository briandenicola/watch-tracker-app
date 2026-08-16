using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStyleAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StyleSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Occasion = table.Column<string>(type: "TEXT", nullable: true),
                    Weather = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StyleSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StyleSessions_Watches_WatchId",
                        column: x => x.WatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StyleRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Occasion = table.Column<string>(type: "TEXT", nullable: true),
                    Weather = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Outfit = table.Column<string>(type: "TEXT", nullable: false),
                    WasHelpful = table.Column<bool>(type: "INTEGER", nullable: true),
                    FeedbackNotes = table.Column<string>(type: "TEXT", nullable: true),
                    FeedbackAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StyleRecommendations_StyleSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StyleSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StyleRecommendations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StyleRecommendations_Watches_WatchId",
                        column: x => x.WatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StyleMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    RecommendationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StyleMessages_StyleRecommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "StyleRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StyleMessages_StyleSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StyleSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StyleMessages_RecommendationId",
                table: "StyleMessages",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_StyleMessages_SessionId_CreatedAt",
                table: "StyleMessages",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StyleRecommendations_SessionId",
                table: "StyleRecommendations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StyleRecommendations_UserId_WatchId_CreatedAt",
                table: "StyleRecommendations",
                columns: new[] { "UserId", "WatchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StyleRecommendations_WatchId",
                table: "StyleRecommendations",
                column: "WatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StyleSessions_UserId_WatchId_UpdatedAt",
                table: "StyleSessions",
                columns: new[] { "UserId", "WatchId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StyleSessions_WatchId",
                table: "StyleSessions",
                column: "WatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StyleMessages");

            migrationBuilder.DropTable(
                name: "StyleRecommendations");

            migrationBuilder.DropTable(
                name: "StyleSessions");
        }
    }
}
