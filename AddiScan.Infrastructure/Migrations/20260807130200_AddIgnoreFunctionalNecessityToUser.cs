using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddiScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnoreFunctionalNecessityToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreFunctionalNecessity",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreFunctionalNecessity",
                table: "Users");
        }
    }
}
