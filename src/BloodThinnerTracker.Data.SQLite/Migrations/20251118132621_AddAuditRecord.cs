using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Data.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityPublicId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PerformedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_EntityPublicId",
                table: "AuditRecords",
                column: "EntityPublicId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_EntityType",
                table: "AuditRecords",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_PerformedBy",
                table: "AuditRecords",
                column: "PerformedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRecords");
        }
    }
}
