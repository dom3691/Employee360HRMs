using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollEngineAndRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PensionPin",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PfaName",
                table: "Employees",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "EmployeeBankAccounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeductionType = table.Column<int>(type: "int", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PercentOfGross = table.Column<decimal>(type: "decimal(8,6)", precision: 8, scale: 6, nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDeductions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RunByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    ProcessedEmployees = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CalculationJobId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollStatutoryRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    EmployeeRate = table.Column<decimal>(type: "decimal(8,6)", precision: 8, scale: 6, nullable: true),
                    EmployerRate = table.Column<decimal>(type: "decimal(8,6)", precision: 8, scale: 6, nullable: true),
                    FixedAnnualAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VariableRate = table.Column<decimal>(type: "decimal(8,6)", precision: 8, scale: 6, nullable: true),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollStatutoryRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Basic = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Housing = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Transport = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryStructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    BandOrder = table.Column<int>(type: "int", nullable: false),
                    UpperBoundAnnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(8,6)", precision: 8, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Basic = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Housing = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Transport = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayFactor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CraMonthly = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Paye = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PensionEmployee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PensionEmployer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Nhf = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NsitfEmployer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomDeductionsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomDeductionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PdfPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payslips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payslips_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaryStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Basic = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Housing = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Transport = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaries_SalaryStructures_SalaryStructureId",
                        column: x => x.SalaryStructureId,
                        principalTable: "SalaryStructures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_EmployeeId_EffectiveDate",
                table: "EmployeeSalaries",
                columns: new[] { "EmployeeId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_SalaryStructureId",
                table: "EmployeeSalaries",
                column: "SalaryStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductions_EmployeeId",
                table: "PayrollDeductions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PeriodYear_PeriodMonth",
                table: "PayrollRuns",
                columns: new[] { "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollStatutoryRates_TaxYear_Code",
                table: "PayrollStatutoryRates",
                columns: new[] { "TaxYear", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId",
                table: "Payslips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayrollRunId_EmployeeId",
                table: "Payslips",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructures_Name",
                table: "SalaryStructures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxBands_TaxYear_BandOrder",
                table: "TaxBands",
                columns: new[] { "TaxYear", "BandOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSalaries");

            migrationBuilder.DropTable(
                name: "PayrollDeductions");

            migrationBuilder.DropTable(
                name: "PayrollStatutoryRates");

            migrationBuilder.DropTable(
                name: "Payslips");

            migrationBuilder.DropTable(
                name: "TaxBands");

            migrationBuilder.DropTable(
                name: "SalaryStructures");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "PensionPin",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PfaName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "EmployeeBankAccounts");
        }
    }
}
