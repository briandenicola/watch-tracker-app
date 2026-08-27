using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistPriceMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PriceAlertEnabled",
                table: "Watches",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAlertTarget",
                table: "Watches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceCheckedAt",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PriceObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderListingId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ListingKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ListingUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ListingTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    MatchConfidence = table.Column<string>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ObservedOnUtc = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceObservations", x => x.Id);
                    table.CheckConstraint("CK_PriceObservations_Currency", "\"Currency\" = 'USD'");
                    table.CheckConstraint("CK_PriceObservations_Price", "\"Price\" > 0");
                    table.ForeignKey(
                        name: "FK_PriceObservations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceObservations_Watches_WatchId",
                        column: x => x.WatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PriceObservationId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_PriceObservations_PriceObservationId",
                        column: x => x.PriceObservationId,
                        principalTable: "PriceObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Watches_IsWishList_PriceAlertEnabled_PriceCheckedAt",
                table: "Watches",
                columns: new[] { "IsWishList", "PriceAlertEnabled", "PriceCheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_PriceObservationId_Trigger",
                table: "PriceAlerts",
                columns: new[] { "PriceObservationId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId_IsRead_CreatedAt",
                table: "PriceAlerts",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceObservations_UserId",
                table: "PriceObservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceObservations_WatchId_ObservedAt",
                table: "PriceObservations",
                columns: new[] { "WatchId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceObservations_WatchId_Source_ListingKey_Price_ObservedOnUtc",
                table: "PriceObservations",
                columns: new[] { "WatchId", "Source", "ListingKey", "Price", "ObservedOnUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "PriceObservations");

            migrationBuilder.DropIndex(
                name: "IX_Watches_IsWishList_PriceAlertEnabled_PriceCheckedAt",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "PriceAlertEnabled",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "PriceAlertTarget",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "PriceCheckedAt",
                table: "Watches");
        }
    }
}
