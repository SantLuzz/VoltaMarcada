using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceSync.Infrastructure.Persistence;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260818190000_EnforceSingleCompanyMembership")]
public partial class EnforceSingleCompanyMembership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_CompanyUsers_UserId_CompanyId",
            table: "CompanyUsers");

        migrationBuilder.CreateIndex(
            name: "UX_CompanyUsers_UserId",
            table: "CompanyUsers",
            column: "UserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_CompanyUsers_UserId",
            table: "CompanyUsers");

        migrationBuilder.CreateIndex(
            name: "UX_CompanyUsers_UserId_CompanyId",
            table: "CompanyUsers",
            columns: new[] { "UserId", "CompanyId" },
            unique: true);
    }
}
