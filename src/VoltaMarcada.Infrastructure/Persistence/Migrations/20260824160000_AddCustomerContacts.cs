using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824160000_AddCustomerContacts")]
public partial class AddCustomerContacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "CustomerContacts", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
            CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
            OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
            Channel = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_CustomerContacts", x => x.Id);
            table.ForeignKey("FK_CustomerContacts_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerContacts_Customers_CompanyId_CustomerId", x => new { x.CompanyId, x.CustomerId }, "Customers", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerContacts_MaintenanceOpportunities_CompanyId_OpportunityId", x => new { x.CompanyId, x.OpportunityId }, "MaintenanceOpportunities", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_CustomerContacts_CompanyId_OpportunityId_CreatedAtUtc", "CustomerContacts", new[] { "CompanyId", "OpportunityId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_CustomerContacts_CompanyId_CustomerId_CreatedAtUtc", "CustomerContacts", new[] { "CompanyId", "CustomerId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "CustomerContacts");
}
