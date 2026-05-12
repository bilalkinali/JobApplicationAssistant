using JobApplicationAssistant.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512173000_AddApplicationGapDecisions")]
    public partial class AddApplicationGapDecisions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GapDecisions",
                table: "job_applications",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GapDecisions",
                table: "job_applications");
        }
    }
}
