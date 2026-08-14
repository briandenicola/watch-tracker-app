using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchAcquisitionAndDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcquiredFrom",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionSourceUrl",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionType",
                table: "Watches",
                type: "TEXT",
                nullable: false,
                defaultValue: "New");

            migrationBuilder.CreateTable(
                name: "WatchDispositions",
                columns: table => new
                {
                    WatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    DispositionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SoldTo = table.Column<string>(type: "TEXT", nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReceivedWatchId = table.Column<int>(type: "INTEGER", nullable: true),
                    TradeDetails = table.Column<string>(type: "TEXT", nullable: true),
                    OtherLabel = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnReason = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnedTo = table.Column<string>(type: "TEXT", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchDispositions", x => x.WatchId);
                    table.ForeignKey(
                        name: "FK_WatchDispositions_Watches_ReceivedWatchId",
                        column: x => x.ReceivedWatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WatchDispositions_Watches_WatchId",
                        column: x => x.WatchId,
                        principalTable: "Watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchDispositions_ReceivedWatchId",
                table: "WatchDispositions",
                column: "ReceivedWatchId");

            migrationBuilder.Sql(
                """
                INSERT INTO WatchDispositions (WatchId, Type, DispositionDate)
                SELECT Id, 'Retired', COALESCE(RetiredAt, UpdatedAt)
                FROM Watches
                WHERE IsRetired = 1
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE "__DispositionRollbackGuard" (
                    "Allowed" INTEGER NOT NULL CHECK ("Allowed" = 1)
                );
                INSERT INTO "__DispositionRollbackGuard" ("Allowed")
                SELECT 0
                WHERE EXISTS (
                    SELECT 1
                    FROM WatchDispositions
                    WHERE Type <> 'Retired'
                );
                DROP TABLE "__DispositionRollbackGuard";
                """);

            migrationBuilder.Sql(
                """
                UPDATE Watches
                SET IsRetired = 1,
                    RetiredAt = (
                        SELECT DispositionDate
                        FROM WatchDispositions
                        WHERE WatchDispositions.WatchId = Watches.Id
                    )
                WHERE EXISTS (
                    SELECT 1
                    FROM WatchDispositions
                    WHERE WatchDispositions.WatchId = Watches.Id
                      AND WatchDispositions.Type = 'Retired'
                )
                """);

            migrationBuilder.DropTable(
                name: "WatchDispositions");

            migrationBuilder.DropColumn(
                name: "AcquiredFrom",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "AcquisitionSourceUrl",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "AcquisitionType",
                table: "Watches");
        }
    }
}
