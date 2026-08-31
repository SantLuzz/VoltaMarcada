using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260831120000_RepairCompanyBusinessHours")]
public partial class RepairCompanyBusinessHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Companies"
                ADD COLUMN IF NOT EXISTS "PublicRequestIntervalMinutes" integer;

            UPDATE "Companies"
            SET "PublicRequestIntervalMinutes" = 60
            WHERE "PublicRequestIntervalMinutes" IS NULL
               OR "PublicRequestIntervalMinutes" <= 0
               OR MOD(60, NULLIF("PublicRequestIntervalMinutes", 0)) <> 0;

            ALTER TABLE "Companies"
                ALTER COLUMN "PublicRequestIntervalMinutes" SET DEFAULT 60,
                ALTER COLUMN "PublicRequestIntervalMinutes" SET NOT NULL;

            CREATE TABLE IF NOT EXISTS "CompanyBusinessHours"
            (
                "CompanyId" uuid NOT NULL,
                "DayOfWeek" integer NOT NULL,
                "IsOpen" boolean NOT NULL,
                "StartTime" time without time zone NULL,
                "EndTime" time without time zone NULL
            );

            DO $migration$
            BEGIN
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'PK_CompanyBusinessHours'
                      AND conrelid = '"CompanyBusinessHours"'::regclass
                ) THEN
                    ALTER TABLE "CompanyBusinessHours"
                        ADD CONSTRAINT "PK_CompanyBusinessHours" PRIMARY KEY ("CompanyId", "DayOfWeek");
                END IF;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'FK_CompanyBusinessHours_Companies_CompanyId'
                      AND conrelid = '"CompanyBusinessHours"'::regclass
                ) THEN
                    ALTER TABLE "CompanyBusinessHours"
                        ADD CONSTRAINT "FK_CompanyBusinessHours_Companies_CompanyId"
                        FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
                END IF;
            END
            $migration$;

            INSERT INTO "CompanyBusinessHours" ("CompanyId", "DayOfWeek", "IsOpen", "StartTime", "EndTime")
            SELECT
                company."Id",
                day."DayOfWeek",
                day."DayOfWeek" BETWEEN 1 AND 5,
                CASE WHEN day."DayOfWeek" BETWEEN 1 AND 5 THEN TIME '08:00' ELSE NULL END,
                CASE WHEN day."DayOfWeek" BETWEEN 1 AND 5 THEN TIME '18:00' ELSE NULL END
            FROM "Companies" AS company
            CROSS JOIN generate_series(0, 6) AS day("DayOfWeek")
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM "CompanyBusinessHours" AS existing
                WHERE existing."CompanyId" = company."Id"
                  AND existing."DayOfWeek" = day."DayOfWeek"
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
