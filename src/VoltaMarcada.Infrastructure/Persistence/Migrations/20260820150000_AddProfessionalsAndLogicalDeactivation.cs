using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

public partial class AddProfessionalsAndLogicalDeactivation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Customers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Equipment",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.DropIndex(
            name: "IX_CompanyUsers_CompanyId",
            table: "CompanyUsers");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_CompanyUsers_CompanyId_Id",
            table: "CompanyUsers",
            columns: new[] { "CompanyId", "Id" });

        migrationBuilder.CreateTable(
            name: "Professionals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Professionals", x => x.Id);
                table.UniqueConstraint("AK_Professionals_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey(
                    name: "FK_Professionals_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Professionals_CompanyUsers_CompanyId_CompanyUserId",
                    columns: x => new { x.CompanyId, x.CompanyUserId },
                    principalTable: "CompanyUsers",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProfessionalServices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProfessionalServices", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProfessionalServices_Professionals_CompanyId_ProfessionalId",
                    columns: x => new { x.CompanyId, x.ProfessionalId },
                    principalTable: "Professionals",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProfessionalServices_Services_CompanyId_ServiceId",
                    columns: x => new { x.CompanyId, x.ServiceId },
                    principalTable: "Services",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Professionals_CompanyId_IsActive",
            table: "Professionals",
            columns: new[] { "CompanyId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "UX_Professionals_CompanyId_CompanyUserId",
            table: "Professionals",
            columns: new[] { "CompanyId", "CompanyUserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_ProfessionalServices_CompanyId_ProfessionalId_ServiceId",
            table: "ProfessionalServices",
            columns: new[] { "CompanyId", "ProfessionalId", "ServiceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProfessionalServices_CompanyId_ServiceId",
            table: "ProfessionalServices",
            columns: new[] { "CompanyId", "ServiceId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProfessionalServices");
        migrationBuilder.DropTable(name: "Professionals");
        migrationBuilder.DropUniqueConstraint(name: "AK_CompanyUsers_CompanyId_Id", table: "CompanyUsers");

        migrationBuilder.CreateIndex(
            name: "IX_CompanyUsers_CompanyId",
            table: "CompanyUsers",
            column: "CompanyId");

        migrationBuilder.DropColumn(name: "IsActive", table: "Customers");
        migrationBuilder.DropColumn(name: "IsActive", table: "Equipment");
    }
}
