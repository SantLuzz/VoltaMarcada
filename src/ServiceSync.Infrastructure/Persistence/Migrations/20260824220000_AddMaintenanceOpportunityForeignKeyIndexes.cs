using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824220000_AddMaintenanceOpportunityForeignKeyIndexes")]
public partial class AddMaintenanceOpportunityForeignKeyIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_EquipmentId",
            table: "MaintenanceOpportunities",
            columns: new[] { "CompanyId", "EquipmentId" });

        migrationBuilder.CreateIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_PreviousWorkOrderId",
            table: "MaintenanceOpportunities",
            columns: new[] { "CompanyId", "PreviousWorkOrderId" });

        migrationBuilder.CreateIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_RecommendedServiceId",
            table: "MaintenanceOpportunities",
            columns: new[] { "CompanyId", "RecommendedServiceId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_EquipmentId",
            table: "MaintenanceOpportunities");

        migrationBuilder.DropIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_PreviousWorkOrderId",
            table: "MaintenanceOpportunities");

        migrationBuilder.DropIndex(
            name: "IX_MaintenanceOpportunities_CompanyId_RecommendedServiceId",
            table: "MaintenanceOpportunities");
    }
}
