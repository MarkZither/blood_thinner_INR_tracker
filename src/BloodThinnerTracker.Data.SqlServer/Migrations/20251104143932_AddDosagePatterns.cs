using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDosagePatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DosagePatternId",
                table: "MedicationLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedDosage",
                table: "MedicationLogs",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatternDayNumber",
                table: "MedicationLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicationDosagePatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    PatternSequence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationDosagePatterns", x => x.Id);
                    table.CheckConstraint("CK_MedicationDosagePattern_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_MedicationDosagePatterns_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_DosagePatternId",
                table: "MedicationLogs",
                column: "DosagePatternId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicationLog_ExpectedDosage",
                table: "MedicationLogs",
                sql: "\"ExpectedDosage\" IS NULL OR (\"ExpectedDosage\" > 0 AND \"ExpectedDosage\" <= 1000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicationLog_PatternDay",
                table: "MedicationLogs",
                sql: "\"PatternDayNumber\" IS NULL OR (\"PatternDayNumber\" >= 1 AND \"PatternDayNumber\" <= 365)");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDosagePattern_Active",
                table: "MedicationDosagePatterns",
                columns: new[] { "MedicationId", "EndDate" },
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDosagePattern_Temporal",
                table: "MedicationDosagePatterns",
                columns: new[] { "MedicationId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDosagePatterns_PublicId",
                table: "MedicationDosagePatterns",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationLogs_MedicationDosagePatterns_DosagePatternId",
                table: "MedicationLogs",
                column: "DosagePatternId",
                principalTable: "MedicationDosagePatterns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationLogs_MedicationDosagePatterns_DosagePatternId",
                table: "MedicationLogs");

            migrationBuilder.DropTable(
                name: "MedicationDosagePatterns");

            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_DosagePatternId",
                table: "MedicationLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicationLog_ExpectedDosage",
                table: "MedicationLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicationLog_PatternDay",
                table: "MedicationLogs");

            migrationBuilder.DropColumn(
                name: "DosagePatternId",
                table: "MedicationLogs");

            migrationBuilder.DropColumn(
                name: "ExpectedDosage",
                table: "MedicationLogs");

            migrationBuilder.DropColumn(
                name: "PatternDayNumber",
                table: "MedicationLogs");
        }
    }
}
