using JobApplicationAssistant.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260516000000_StoreAiRunSummariesAsText")]
public partial class StoreAiRunSummariesAsText : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "InputSummary",
            table: "ai_runs",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "jsonb",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "OutputSummary",
            table: "ai_runs",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "jsonb",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "InputSummary",
            table: "ai_runs",
            type: "jsonb",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "OutputSummary",
            table: "ai_runs",
            type: "jsonb",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
