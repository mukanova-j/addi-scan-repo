using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddiScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Additives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ENumber = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameVerbatim = table.Column<string>(type: "TEXT", nullable: false),
                    RegulatoryNoteFromSource = table.Column<string>(type: "TEXT", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    FoundIn = table.Column<string>(type: "TEXT", nullable: true),
                    SourceOrigin = table.Column<string>(type: "TEXT", nullable: true),
                    HealthConcerns = table.Column<string>(type: "TEXT", nullable: true),
                    SideEffects = table.Column<string>(type: "TEXT", nullable: true),
                    BannedAnywhere = table.Column<string>(type: "TEXT", nullable: true),
                    CommonNamesAndSynonyms = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceSources = table.Column<string>(type: "TEXT", nullable: false),
                    LastResearched = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Grading_Graded = table.Column<bool>(type: "INTEGER", nullable: true),
                    Grading_FinalScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Grading_RawPoints = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_RiskBand = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_CarcinogenicityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_CarcinogenicityNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_BanStatusScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_BanStatusNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_AllergicReactionsScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_AllergicReactionsNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_CumulativeRiskScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_CumulativeRiskNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_OriginScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_OriginNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_ChildrenAndVulnerableScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_ChildrenAndVulnerableNote = table.Column<string>(type: "TEXT", nullable: true),
                    Grading_FunctionalNecessityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Grading_FunctionalNecessityNote = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Additives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsentGivenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Additives");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
