using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodThinnerTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedModelProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailNotificationsEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPushNotificationsEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmsNotificationsEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Users",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileCompletedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderAdvanceMinutes",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Medications",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contraindications",
                table: "Medications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Form",
                table: "Medications",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "INRTargetMax",
                table: "Medications",
                type: "decimal(3,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "INRTargetMin",
                table: "Medications",
                type: "decimal(3,1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Imprint",
                table: "Medications",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBloodThinner",
                table: "Medications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDailyDose",
                table: "Medications",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinHoursBetweenDoses",
                table: "Medications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresINRMonitoring",
                table: "Medications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "Medications",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageInstructions",
                table: "Medications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPushNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSmsNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileCompletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReminderAdvanceMinutes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Contraindications",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Form",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "INRTargetMax",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "INRTargetMin",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Imprint",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "IsBloodThinner",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "MaxDailyDose",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "MinHoursBetweenDoses",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "RequiresINRMonitoring",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "StorageInstructions",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Medications");
        }
    }
}
