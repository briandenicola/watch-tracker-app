using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWearTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastWornDate",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesWorn",
                table: "Watches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWornDate",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "TimesWorn",
                table: "Watches");
        }
    }
}
