using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820090000_AddEmployeeInvitations")]
public partial class AddEmployeeInvitations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeInvitations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Document = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeInvitations", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmployeeInvitations_AspNetUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_EmployeeInvitations_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeInvitations_CompanyId_CreatedAtUtc",
            table: "EmployeeInvitations",
            columns: new[] { "CompanyId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeInvitations_CompanyId_Document_State",
            table: "EmployeeInvitations",
            columns: new[] { "CompanyId", "Document", "AcceptedAtUtc", "RevokedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeInvitations_CompanyId_Email_State",
            table: "EmployeeInvitations",
            columns: new[] { "CompanyId", "Email", "AcceptedAtUtc", "RevokedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeInvitations_CreatedByUserId",
            table: "EmployeeInvitations",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UX_EmployeeInvitations_TokenHash",
            table: "EmployeeInvitations",
            column: "TokenHash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmployeeInvitations");
    }
}
