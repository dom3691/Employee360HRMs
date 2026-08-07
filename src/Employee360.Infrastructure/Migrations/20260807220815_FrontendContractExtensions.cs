using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FrontendContractExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HolidayType",
                table: "PublicHolidays",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "PublicHolidays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "PublicHolidays",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowHalfDay",
                table: "LeaveTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "LeaveTypes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentThresholdDays",
                table: "LeaveTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticeDays",
                table: "LeaveTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PolicyDescription",
                table: "LeaveTypes",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "LeaveTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "JobPostings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiringManagerName",
                table: "JobPostings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobPostings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PostedDate",
                table: "JobPostings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "JobPostings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMax",
                table: "JobPostings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMin",
                table: "JobPostings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "JobPostings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EmailTemplates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "System");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "EmailTemplates",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VariablesJson",
                table: "EmailTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Companies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Companies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfigurationJson",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Companies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Companies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingName",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingCalendarJson",
                table: "Companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HolidayType",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "AllowHalfDay",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DocumentThresholdDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "NoticeDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "PolicyDescription",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "HiringManagerName",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryMax",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryMin",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "VariablesJson",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "EmailConfigurationJson",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TradingName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorkingCalendarJson",
                table: "Companies");
        }
    }
}
