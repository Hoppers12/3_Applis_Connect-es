using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BDD.Migrations
{
    /// <inheritdoc />
    public partial class AddBooleanColumnsToResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPair",
                table: "Results",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsParfait",
                table: "Results",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremier",
                table: "Results",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPair",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "IsParfait",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "IsPremier",
                table: "Results");
        }
    }
}
