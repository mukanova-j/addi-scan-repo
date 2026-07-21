using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddiScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanRecordMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdditiveId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchedTerm = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    ScanRecordId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRecordMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanRecordMatches_ScanRecords_ScanRecordId",
                        column: x => x.ScanRecordId,
                        principalTable: "ScanRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanRecordMatches_ScanRecordId",
                table: "ScanRecordMatches",
                column: "ScanRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanRecords_UserId",
                table: "ScanRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanRecordMatches");

            migrationBuilder.DropTable(
                name: "ScanRecords");
        }
    }
}
