using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediumPriorityContractExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComponentsJson",
                table: "SalaryStructures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeCodes",
                table: "SalaryStructures",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Grades",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Grades",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LevelRank",
                table: "Grades",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentTitle",
                table: "Candidates",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Candidates",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Candidates",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""UPDATE "Grades" SET "Code" = 'G' || "Level"::text WHERE "Code" IS NULL;""");
            migrationBuilder.Sql("""UPDATE "Grades" SET "LevelRank" = 'Level ' || "Level"::text WHERE "LevelRank" IS NULL;""");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Grades",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LevelRank",
                table: "Grades",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Code",
                table: "Grades",
                column: "Code",
                unique: true);

            // CandidateStage.Assessment inserted at value 3 — shift Offer/Hired/Rejected up by one.
            migrationBuilder.Sql("""UPDATE "Candidates" SET "Stage" = "Stage" + 1 WHERE "Stage" >= 3;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Candidates" SET "Stage" = "Stage" - 1 WHERE "Stage" >= 4;""");

            migrationBuilder.DropIndex(
                name: "IX_Grades_Code",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ComponentsJson",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "GradeCodes",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "LevelRank",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CurrentTitle",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Candidates");
        }
    }
}
