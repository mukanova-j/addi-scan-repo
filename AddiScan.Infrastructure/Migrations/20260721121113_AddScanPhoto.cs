using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddiScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "ScanRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "ScanRecords",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "ScanRecords");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "ScanRecords");
        }
    }
}
