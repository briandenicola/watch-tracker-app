using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Watches_UserId",
                table: "Watches");

            migrationBuilder.AddColumn<int>(
                name: "WishlistPriority",
                table: "Watches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Watches
                SET WishlistPriority = (
                    SELECT COUNT(*)
                    FROM Watches AS Earlier
                    WHERE Earlier.UserId = Watches.UserId
                      AND Earlier.IsWishList = 1
                      AND (
                          Earlier.CreatedAt > Watches.CreatedAt
                          OR (Earlier.CreatedAt = Watches.CreatedAt AND Earlier.Id > Watches.Id)
                      )
                )
                WHERE IsWishList = 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Watches_UserId_WishlistPriority",
                table: "Watches",
                columns: new[] { "UserId", "WishlistPriority" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Watches_UserId_WishlistPriority",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "WishlistPriority",
                table: "Watches");

            migrationBuilder.CreateIndex(
                name: "IX_Watches_UserId",
                table: "Watches",
                column: "UserId");
        }
    }
}
