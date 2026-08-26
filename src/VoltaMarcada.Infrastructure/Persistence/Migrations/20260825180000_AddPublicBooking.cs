using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825180000_AddPublicBooking")]
public partial class AddPublicBooking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Origin", table: "Appointments", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "PublicDescription", table: "Companies", type: "character varying(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PublicPhone", table: "Companies", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>(name: "NotificationEmail", table: "Companies", type: "character varying(320)", maxLength: 320, nullable: true);
        migrationBuilder.AddColumn<string>(name: "BookingTokenHash", table: "MaintenanceOpportunities", type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "BookingTokenExpiresAtUtc", table: "MaintenanceOpportunities", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "BookingTokenRevokedAtUtc", table: "MaintenanceOpportunities", type: "timestamp with time zone", nullable: true);

        migrationBuilder.CreateTable(
            name: "AppointmentRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                MaintenanceOpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                RequestedStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                RequestedStartAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RequestedEndAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                ProposedDate = table.Column<DateOnly>(type: "date", nullable: true),
                ProposedStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                ProposedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                ProposedProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ProposedCustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ProposedCustomerWhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                ProposedCustomerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                ProposedAddressType = table.Column<int>(type: "integer", nullable: true),
                ProposedAddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ProposedAddressNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ProposedAddressPostalCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                ProposedAddressComplement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ProposedAddressStateId = table.Column<Guid>(type: "uuid", nullable: true),
                ProposedAddressCityId = table.Column<Guid>(type: "uuid", nullable: true),
                ResponseTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                ResponseTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ResponseTokenRevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppointmentRequests", x => x.Id);
                table.UniqueConstraint("AK_AppointmentRequests_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey("FK_AppointmentRequests_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Customers_CompanyId_CustomerId", x => new { x.CompanyId, x.CustomerId }, "Customers", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Services_CompanyId_ServiceId", x => new { x.CompanyId, x.ServiceId }, "Services", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Professionals_CompanyId_ProfessionalId", x => new { x.CompanyId, x.ProfessionalId }, "Professionals", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Professionals_CompanyId_ProposedProfessionalId", x => new { x.CompanyId, x.ProposedProfessionalId }, "Professionals", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Equipment_CompanyId_EquipmentId", x => new { x.CompanyId, x.EquipmentId }, "Equipment", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_MaintenanceOpportunities_CompanyId_MaintenanceOpportunityId", x => new { x.CompanyId, x.MaintenanceOpportunityId }, "MaintenanceOpportunities", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Appointments_CompanyId_AppointmentId", x => new { x.CompanyId, x.AppointmentId }, "Appointments", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_States_ProposedAddressStateId", x => x.ProposedAddressStateId, "States", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentRequests_Cities_ProposedAddressCityId", x => x.ProposedAddressCityId, "Cities", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_MaintenanceOpportunities_BookingTokenHash", table: "MaintenanceOpportunities", column: "BookingTokenHash", unique: true, filter: "\"BookingTokenHash\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_Status_RequestedStartAtUtc", table: "AppointmentRequests", columns: new[] { "CompanyId", "Status", "RequestedStartAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_CustomerId", table: "AppointmentRequests", columns: new[] { "CompanyId", "CustomerId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_ServiceId", table: "AppointmentRequests", columns: new[] { "CompanyId", "ServiceId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_ProfessionalId", table: "AppointmentRequests", columns: new[] { "CompanyId", "ProfessionalId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_ProposedProfessionalId", table: "AppointmentRequests", columns: new[] { "CompanyId", "ProposedProfessionalId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_EquipmentId", table: "AppointmentRequests", columns: new[] { "CompanyId", "EquipmentId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_MaintenanceOpportunityId", table: "AppointmentRequests", columns: new[] { "CompanyId", "MaintenanceOpportunityId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_CompanyId_AppointmentId", table: "AppointmentRequests", columns: new[] { "CompanyId", "AppointmentId" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_ProposedAddressStateId", table: "AppointmentRequests", column: "ProposedAddressStateId");
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_ProposedAddressCityId", table: "AppointmentRequests", column: "ProposedAddressCityId");
        migrationBuilder.CreateIndex(name: "IX_AppointmentRequests_ResponseTokenHash", table: "AppointmentRequests", column: "ResponseTokenHash", unique: true, filter: "\"ResponseTokenHash\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentRequests");
        migrationBuilder.DropIndex(name: "IX_MaintenanceOpportunities_BookingTokenHash", table: "MaintenanceOpportunities");
        migrationBuilder.DropColumn(name: "Origin", table: "Appointments");
        migrationBuilder.DropColumn(name: "PublicDescription", table: "Companies");
        migrationBuilder.DropColumn(name: "PublicPhone", table: "Companies");
        migrationBuilder.DropColumn(name: "NotificationEmail", table: "Companies");
        migrationBuilder.DropColumn(name: "BookingTokenHash", table: "MaintenanceOpportunities");
        migrationBuilder.DropColumn(name: "BookingTokenExpiresAtUtc", table: "MaintenanceOpportunities");
        migrationBuilder.DropColumn(name: "BookingTokenRevokedAtUtc", table: "MaintenanceOpportunities");
    }
}
