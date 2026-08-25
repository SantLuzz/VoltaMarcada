using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820100000_AddApplicationUserProfile")]
public partial class AddApplicationUserProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "AspNetUsers",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Document",
            table: "AspNetUsers",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FullName",
            table: "AspNetUsers",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Phone",
            table: "AspNetUsers",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "UX_AspNetUsers_Document",
            table: "AspNetUsers",
            column: "Document",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "UX_AspNetUsers_Document", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "Address", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "Document", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "FullName", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "Phone", table: "AspNetUsers");
    }
}
