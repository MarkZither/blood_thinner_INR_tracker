using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Data.SQLite.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20251217120000_AddMedicationIsActiveIndex")]
    public partial class AddMedicationIsActiveIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite supports IF NOT EXISTS for CREATE INDEX
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Medications_IsActive ON \"Medications\" (\"IsActive\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Medications_IsActive;");
        }
    }
}
