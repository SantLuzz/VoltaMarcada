using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceSync.Infrastructure.Persistence;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822130000_RemoveApplicationUserDocumentUniqueIndex")]
public partial class RemoveApplicationUserDocumentUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_AspNetUsers_Document",
            table: "AspNetUsers");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "UX_AspNetUsers_Document",
            table: "AspNetUsers",
            column: "Document",
            unique: true);
    }
}
