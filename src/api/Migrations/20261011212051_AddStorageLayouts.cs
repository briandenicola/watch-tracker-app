using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StorageSlot",
                table: "Watches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageLayoutsJson",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageSlot",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "StorageLayoutsJson",
                table: "Users");
        }
    }
}
