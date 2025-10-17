﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateMedicalEntitiesFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    TimeZone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<int>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AuthProvider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ExternalUserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    HealthcareProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HealthcareProviderPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    MedicalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Preferences = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "INRTests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    INRValue = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    TargetINRMin = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    TargetINRMax = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    ProthrombinTime = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PartialThromboplastinTime = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Laboratory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TestMethod = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IsPointOfCare = table.Column<bool>(type: "INTEGER", nullable: false),
                    WasFasting = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastMedicationTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MedicationsTaken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FoodsConsumed = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HealthConditions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RecommendedActions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DosageChanges = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextTestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByProvider = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PatientNotified = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INRTests", x => x.Id);
                    table.CheckConstraint("CK_INRTest_PT", "ProthrombinTime IS NULL OR (ProthrombinTime >= 8.0 AND ProthrombinTime <= 60.0)");
                    table.CheckConstraint("CK_INRTest_PTT", "PartialThromboplastinTime IS NULL OR (PartialThromboplastinTime >= 20.0 AND PartialThromboplastinTime <= 120.0)");
                    table.CheckConstraint("CK_INRTest_TargetRange", "TargetINRMin IS NULL OR TargetINRMax IS NULL OR TargetINRMin < TargetINRMax");
                    table.CheckConstraint("CK_INRTest_Value", "INRValue >= 0.5 AND INRValue <= 8.0");
                    table.ForeignKey(
                        name: "FK_INRTests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Dosage = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    DosageUnit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomFrequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ScheduledTimes = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrescribedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescriptionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Pharmacy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescriptionNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FoodInteractions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DrugInteractions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SideEffects = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RemindersEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReminderMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                    table.CheckConstraint("CK_Medication_Dates", "EndDate IS NULL OR EndDate >= StartDate");
                    table.CheckConstraint("CK_Medication_Dosage", "Dosage > 0 AND Dosage <= 1000");
                    table.CheckConstraint("CK_Medication_Reminder", "ReminderMinutes >= 0 AND ReminderMinutes <= 1440");
                    table.ForeignKey(
                        name: "FK_Medications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INRSchedules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreferredTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetINRMin = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    TargetINRMax = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    PrescribedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescribedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreferredLaboratory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LaboratoryContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TestingInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RemindersEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReminderDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderMethods = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedTestId = table.Column<string>(type: "TEXT", nullable: true),
                    ModificationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentScheduleId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INRSchedules", x => x.Id);
                    table.CheckConstraint("CK_INRSchedule_Dates", "EndDate IS NULL OR EndDate > ScheduledDate");
                    table.CheckConstraint("CK_INRSchedule_Interval", "IntervalDays >= 1 AND IntervalDays <= 365");
                    table.CheckConstraint("CK_INRSchedule_Reminder", "ReminderDays >= 0 AND ReminderDays <= 14");
                    table.CheckConstraint("CK_INRSchedule_TargetRange", "TargetINRMin IS NULL OR TargetINRMax IS NULL OR TargetINRMin < TargetINRMax");
                    table.ForeignKey(
                        name: "FK_INRSchedules_INRSchedules_ParentScheduleId",
                        column: x => x.ParentScheduleId,
                        principalTable: "INRSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_INRSchedules_INRTests_CompletedTestId",
                        column: x => x.CompletedTestId,
                        principalTable: "INRTests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_INRSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MedicationId = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualDosage = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    ActualDosageUnit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SideEffects = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EntryMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryDevice = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ConfirmedByProvider = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TakenWithFood = table.Column<bool>(type: "INTEGER", nullable: true),
                    FoodDetails = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TimeVarianceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationLogs", x => x.Id);
                    table.CheckConstraint("CK_MedicationLog_ActualDosage", "ActualDosage IS NULL OR (ActualDosage > 0 AND ActualDosage <= 1000)");
                    table.CheckConstraint("CK_MedicationLog_TimeVariance", "TimeVarianceMinutes >= -1440 AND TimeVarianceMinutes <= 1440");
                    table.ForeignKey(
                        name: "FK_MedicationLogs_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_INRSchedules_CompletedTestId",
                table: "INRSchedules",
                column: "CompletedTestId");

            migrationBuilder.CreateIndex(
                name: "IX_INRSchedules_ParentScheduleId",
                table: "INRSchedules",
                column: "ParentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_INRSchedules_UserId_ScheduledDate",
                table: "INRSchedules",
                columns: new[] { "UserId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_INRTests_UserId_TestDate",
                table: "INRTests",
                columns: new[] { "UserId", "TestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_MedicationId",
                table: "MedicationLogs",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_UserId_ScheduledTime",
                table: "MedicationLogs",
                columns: new[] { "UserId", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_UserId_Name",
                table: "Medications",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalUserId",
                table: "Users",
                column: "ExternalUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INRSchedules");

            migrationBuilder.DropTable(
                name: "MedicationLogs");

            migrationBuilder.DropTable(
                name: "INRTests");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
