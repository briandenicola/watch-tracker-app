using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWearLogDurationAndWatchMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "WearLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "WearLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBatteryChangedDate",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionYear",
                table: "Watches",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "WearLogs");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "WearLogs");

            migrationBuilder.DropColumn(
                name: "LastBatteryChangedDate",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "ProductionYear",
                table: "Watches");
        }
    }
}
