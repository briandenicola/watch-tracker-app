using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvisorRecommendationFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketplaceItemId",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceProvider",
                table: "Watches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdvisorRecommendationFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderItemId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorRecommendationFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvisorRecommendationFeedback_AdvisorMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "AdvisorMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvisorRecommendationFeedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Watches_UserId_MarketplaceProvider_MarketplaceItemId",
                table: "Watches",
                columns: new[] { "UserId", "MarketplaceProvider", "MarketplaceItemId" },
                unique: true,
                filter: "\"MarketplaceProvider\" IS NOT NULL AND \"MarketplaceItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorRecommendationFeedback_MessageId",
                table: "AdvisorRecommendationFeedback",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorRecommendationFeedback_UserId_MessageId_Provider_ProviderItemId",
                table: "AdvisorRecommendationFeedback",
                columns: new[] { "UserId", "MessageId", "Provider", "ProviderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorRecommendationFeedback_UserId_UpdatedAt",
                table: "AdvisorRecommendationFeedback",
                columns: new[] { "UserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorRecommendationFeedback");

            migrationBuilder.DropIndex(
                name: "IX_Watches_UserId_MarketplaceProvider_MarketplaceItemId",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "MarketplaceItemId",
                table: "Watches");

            migrationBuilder.DropColumn(
                name: "MarketplaceProvider",
                table: "Watches");
        }
    }
}
