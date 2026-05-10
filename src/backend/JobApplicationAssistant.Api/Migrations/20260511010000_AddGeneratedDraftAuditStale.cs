using JobApplicationAssistant.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260511010000_AddGeneratedDraftAuditStale")]
    public partial class AddGeneratedDraftAuditStale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClaimAuditStale",
                table: "generated_drafts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsClaimAuditStale",
                table: "generated_drafts");
        }
    }
}
