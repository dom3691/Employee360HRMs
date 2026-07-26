using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GradeId",
                table: "Positions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeadEmployeeId",
                table: "Departments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_GradeId",
                table: "Positions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadEmployeeId",
                table: "Departments",
                column: "HeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Level",
                table: "Grades",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Name",
                table: "Grades",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_HeadEmployeeId",
                table: "Departments",
                column: "HeadEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Grades_GradeId",
                table: "Positions",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_HeadEmployeeId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Grades_GradeId",
                table: "Positions");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Positions_GradeId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HeadEmployeeId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "HeadEmployeeId",
                table: "Departments");
        }
    }
}
