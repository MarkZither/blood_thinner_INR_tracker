using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantStrengthAndUnitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Medications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Strength",
                table: "Medications",
                type: "decimal(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Medications",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
