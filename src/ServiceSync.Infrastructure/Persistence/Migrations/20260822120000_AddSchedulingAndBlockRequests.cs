using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceSync.Infrastructure.Persistence;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822120000_AddSchedulingAndBlockRequests")]
public partial class AddSchedulingAndBlockRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "TimeZoneId", table: "Professionals", type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "America/Sao_Paulo");

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                StartAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.UniqueConstraint("AK_Appointments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey("FK_Appointments_Professionals_CompanyId_ProfessionalId", x => new { x.CompanyId, x.ProfessionalId }, principalTable: "Professionals", principalColumns: new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Appointments_WorkOrders_CompanyId_WorkOrderId", x => new { x.CompanyId, x.WorkOrderId }, principalTable: "WorkOrders", principalColumns: new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ScheduleBlockRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false), Type = table.Column<int>(type: "integer", nullable: false), StartDate = table.Column<DateOnly>(type: "date", nullable: false), EndDate = table.Column<DateOnly>(type: "date", nullable: false), IsFullDay = table.Column<bool>(type: "boolean", nullable: false), StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true), EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true), Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), Status = table.Column<int>(type: "integer", nullable: false), DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true), DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_ScheduleBlockRequests", x => x.Id); table.UniqueConstraint("AK_ScheduleBlockRequests_CompanyId_Id", x => new { x.CompanyId, x.Id }); table.ForeignKey("FK_ScheduleBlockRequests_Professionals_CompanyId_ProfessionalId", x => new { x.CompanyId, x.ProfessionalId }, principalTable: "Professionals", principalColumns: new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(
            name: "AppointmentRescheduleHistories",
            columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), AppointmentId = table.Column<Guid>(type: "uuid", nullable: false), PreviousStartAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), PreviousEndAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), NewStartAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), NewEndAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false), ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) },
            constraints: table => { table.PrimaryKey("PK_AppointmentRescheduleHistories", x => x.Id); table.ForeignKey("FK_AppointmentRescheduleHistories_Appointments_CompanyId_AppointmentId", x => new { x.CompanyId, x.AppointmentId }, principalTable: "Appointments", principalColumns: new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateIndex(name: "IX_Appointments_CompanyId_ProfessionalId_StartAtUtc", table: "Appointments", columns: new[] { "CompanyId", "ProfessionalId", "StartAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_Appointments_CompanyId_WorkOrderId_StartAtUtc", table: "Appointments", columns: new[] { "CompanyId", "WorkOrderId", "StartAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentRescheduleHistories_CompanyId_AppointmentId_CreatedAtUtc", table: "AppointmentRescheduleHistories", columns: new[] { "CompanyId", "AppointmentId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_ScheduleBlockRequests_CompanyId_Status_CreatedAtUtc", table: "ScheduleBlockRequests", columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_ScheduleBlockRequests_CompanyId_ProfessionalId_StartDate_EndDate", table: "ScheduleBlockRequests", columns: new[] { "CompanyId", "ProfessionalId", "StartDate", "EndDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentRescheduleHistories");
        migrationBuilder.DropTable(name: "ScheduleBlockRequests");
        migrationBuilder.DropTable(name: "Appointments");
        migrationBuilder.DropColumn(name: "TimeZoneId", table: "Professionals");
    }
}
