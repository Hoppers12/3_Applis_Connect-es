using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BDD.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPairAndIsPremier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "val1",
                table: "Results",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "val2",
                table: "Results",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "val1",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "val2",
                table: "Results");
        }
    }
}
