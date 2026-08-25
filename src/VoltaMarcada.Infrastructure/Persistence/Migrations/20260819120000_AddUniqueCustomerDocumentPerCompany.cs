using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819120000_AddUniqueCustomerDocumentPerCompany")]
public partial class AddUniqueCustomerDocumentPerCompany : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "UX_Customers_CompanyId_Document",
            table: "Customers",
            columns: new[] { "CompanyId", "Document" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Customers_CompanyId_Document",
            table: "Customers");
    }
}
