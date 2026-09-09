using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedWatchDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaseMaterial",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CaseThicknessMm",
                table: "Watches",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateComplication",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastServicedDate",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiryDate",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WinderDirection",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinderTpd",
                table: "Watches",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseMaterial",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "CaseThicknessMm",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "DateComplication",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "LastServicedDate",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiryDate",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "WinderDirection",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "WinderTpd",
                table: "Watches");
        }
    }
}
