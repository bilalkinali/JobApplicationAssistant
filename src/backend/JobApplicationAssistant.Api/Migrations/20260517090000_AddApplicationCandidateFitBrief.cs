using JobApplicationAssistant.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260517090000_AddApplicationCandidateFitBrief")]
    public partial class AddApplicationCandidateFitBrief : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateFitBrief",
                table: "job_applications",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateFitBrief",
                table: "job_applications");
        }
    }
}
