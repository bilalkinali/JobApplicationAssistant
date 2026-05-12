using System;
using JobApplicationAssistant.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512100000_AddApplicationPreparationState")]
    public partial class AddApplicationPreparationState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPreparedAt",
                table: "job_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreparationStatus",
                table: "job_applications",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "NotStarted");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPreparedAt",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "PreparationStatus",
                table: "job_applications");
        }
    }
}
