using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820120000_AddEquipment")]
public partial class AddEquipment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "AK_Customers_CompanyId_Id",
            table: "Customers",
            columns: new[] { "CompanyId", "Id" });

        migrationBuilder.CreateTable(
            name: "Equipment",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                SerialNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Capacity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                LocationDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                InstallationDate = table.Column<DateOnly>(type: "date", nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Equipment", x => x.Id);
                table.ForeignKey("FK_Equipment_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Equipment_Customers_CompanyId_CustomerId",
                    x => new { x.CompanyId, x.CustomerId },
                    "Customers",
                    new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Equipment_CompanyId_CustomerId", table: "Equipment", columns: new[] { "CompanyId", "CustomerId" });
        migrationBuilder.CreateIndex(name: "IX_Equipment_CompanyId_Type", table: "Equipment", columns: new[] { "CompanyId", "Type" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Equipment");
        migrationBuilder.DropUniqueConstraint(name: "AK_Customers_CompanyId_Id", table: "Customers");
    }
}
