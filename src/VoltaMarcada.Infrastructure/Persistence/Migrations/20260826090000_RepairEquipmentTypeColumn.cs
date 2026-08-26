using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260826090000_RepairEquipmentTypeColumn")]
public partial class RepairEquipmentTypeColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Equipment"
                ADD COLUMN IF NOT EXISTS "Type" character varying(200);

            UPDATE "Equipment"
            SET "Type" = 'Not specified'
            WHERE "Type" IS NULL OR btrim("Type") = '';

            ALTER TABLE "Equipment"
                ALTER COLUMN "Type" SET NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_Equipment_CompanyId_Type"
                ON "Equipment" ("CompanyId", "Type");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
