using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822140000_AddOpenAppointmentOverlapConstraint")]
public partial class AddOpenAppointmentOverlapConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        migrationBuilder.Sql("""
            ALTER TABLE "Appointments"
            ADD CONSTRAINT "EX_Appointments_OpenProfessionalInterval"
            EXCLUDE USING gist
            ("CompanyId" WITH =, "ProfessionalId" WITH =,
             tstzrange("StartAtUtc", "EndAtUtc", '[)') WITH &&)
            WHERE ("Status" IN (0, 1));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Appointments\" DROP CONSTRAINT \"EX_Appointments_OpenProfessionalInterval\";");
    }
}
