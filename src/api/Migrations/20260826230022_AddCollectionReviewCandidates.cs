using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionReviewCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CandidatesGeneratedAt",
                table: "CollectionReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidatesJson",
                table: "CollectionReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceStatusJson",
                table: "CollectionReviews",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidatesGeneratedAt",
                table: "CollectionReviews");

            migrationBuilder.DropColumn(
                name: "CandidatesJson",
                table: "CollectionReviews");

            migrationBuilder.DropColumn(
                name: "MarketplaceStatusJson",
                table: "CollectionReviews");
        }
    }
}
