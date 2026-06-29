using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WatchTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOidcSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderSubject = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OidcLoginTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodeHash = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcLoginTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OidcLoginTickets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OidcProviderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Authority = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecretProtected = table.Column<string>(type: "TEXT", nullable: true),
                    Scopes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcProviderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OidcStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StateHash = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    CodeVerifierProtected = table.Column<string>(type: "TEXT", nullable: false),
                    NonceHash = table.Column<string>(type: "TEXT", nullable: false),
                    ReturnUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LinkUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OidcStates_Users_LinkUserId",
                        column: x => x.LinkUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_Issuer_ProviderSubject",
                table: "ExternalLogins",
                columns: new[] { "Provider", "Issuer", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId_Provider",
                table: "ExternalLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OidcLoginTickets_CodeHash",
                table: "OidcLoginTickets",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OidcLoginTickets_UserId",
                table: "OidcLoginTickets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OidcProviderSettings_Provider",
                table: "OidcProviderSettings",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OidcStates_LinkUserId",
                table: "OidcStates",
                column: "LinkUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OidcStates_StateHash",
                table: "OidcStates",
                column: "StateHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "OidcLoginTickets");

            migrationBuilder.DropTable(
                name: "OidcProviderSettings");

            migrationBuilder.DropTable(
                name: "OidcStates");
        }
    }
}
