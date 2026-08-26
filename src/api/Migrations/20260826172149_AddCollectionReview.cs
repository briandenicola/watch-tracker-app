using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    StrengthsJson = table.Column<string>(type: "TEXT", nullable: false),
                    WeaknessesJson = table.Column<string>(type: "TEXT", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    FactsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionWatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WishlistWatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WatchesUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReviews_UserId",
                table: "CollectionReviews",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionReviews");
        }
    }
}
