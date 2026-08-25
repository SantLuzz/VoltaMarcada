using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824150000_AddMaintenanceOpportunities")]
public partial class AddMaintenanceOpportunities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "MaintenanceOpportunities", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), CustomerId = table.Column<Guid>(type: "uuid", nullable: false), EquipmentId = table.Column<Guid>(type: "uuid", nullable: false), PreviousWorkOrderId = table.Column<Guid>(type: "uuid", nullable: false), RecommendedServiceId = table.Column<Guid>(type: "uuid", nullable: false), DueDate = table.Column<DateOnly>(type: "date", nullable: false), Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false), Status = table.Column<int>(type: "integer", nullable: false), ContactedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), BookingRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), BookedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), DismissedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_MaintenanceOpportunities", x => x.Id);
            table.UniqueConstraint("AK_MaintenanceOpportunities_CompanyId_Id", x => new { x.CompanyId, x.Id });
            table.ForeignKey("FK_MaintenanceOpportunities_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_MaintenanceOpportunities_Customers_CompanyId_CustomerId", x => new { x.CompanyId, x.CustomerId }, "Customers", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_MaintenanceOpportunities_Equipment_CompanyId_EquipmentId", x => new { x.CompanyId, x.EquipmentId }, "Equipment", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_MaintenanceOpportunities_WorkOrders_CompanyId_PreviousWorkOrderId", x => new { x.CompanyId, x.PreviousWorkOrderId }, "WorkOrders", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_MaintenanceOpportunities_Services_CompanyId_RecommendedServiceId", x => new { x.CompanyId, x.RecommendedServiceId }, "Services", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_MaintenanceOpportunities_CompanyId_Status_DueDate", "MaintenanceOpportunities", new[] { "CompanyId", "Status", "DueDate" });
        migrationBuilder.CreateIndex("IX_MaintenanceOpportunities_CompanyId_CustomerId", "MaintenanceOpportunities", new[] { "CompanyId", "CustomerId" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "MaintenanceOpportunities");
}
