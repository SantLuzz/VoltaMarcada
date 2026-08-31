using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260827103000_MakeAppointmentRequestProfessionalOptional")]
public partial class MakeAppointmentRequestProfessionalOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(name: "ProfessionalId", table: "AppointmentRequests", type: "uuid", nullable: true, oldClrType: typeof(Guid), oldType: "uuid");
        migrationBuilder.AlterColumn<DateTimeOffset>(name: "RequestedStartAtUtc", table: "AppointmentRequests", type: "timestamp with time zone", nullable: true, oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone");
        migrationBuilder.AlterColumn<DateTimeOffset>(name: "RequestedEndAtUtc", table: "AppointmentRequests", type: "timestamp with time zone", nullable: true, oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(name: "ProfessionalId", table: "AppointmentRequests", type: "uuid", nullable: false, defaultValue: Guid.Empty, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<DateTimeOffset>(name: "RequestedStartAtUtc", table: "AppointmentRequests", type: "timestamp with time zone", nullable: false, defaultValue: DateTimeOffset.MinValue, oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone", oldNullable: true);
        migrationBuilder.AlterColumn<DateTimeOffset>(name: "RequestedEndAtUtc", table: "AppointmentRequests", type: "timestamp with time zone", nullable: false, defaultValue: DateTimeOffset.MinValue, oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone", oldNullable: true);
    }
}
