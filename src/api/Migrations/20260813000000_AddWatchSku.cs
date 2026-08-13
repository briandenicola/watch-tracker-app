using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Watches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Watches");
        }
    }
}
