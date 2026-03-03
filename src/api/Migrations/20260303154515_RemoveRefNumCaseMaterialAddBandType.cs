using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRefNumCaseMaterialAddBandType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseMaterial",
                table: "Watches");

            migrationBuilder.RenameColumn(
                name: "ReferenceNumber",
                table: "Watches",
                newName: "BandType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BandType",
                table: "Watches",
                newName: "ReferenceNumber");

            migrationBuilder.AddColumn<string>(
                name: "CaseMaterial",
                table: "Watches",
                type: "TEXT",
                nullable: true);
        }
    }
}
