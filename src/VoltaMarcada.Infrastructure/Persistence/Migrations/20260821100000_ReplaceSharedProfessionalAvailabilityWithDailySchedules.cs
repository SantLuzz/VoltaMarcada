using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260821100000_ReplaceSharedProfessionalAvailabilityWithDailySchedules")]
public partial class ReplaceSharedProfessionalAvailabilityWithDailySchedules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProfessionalDailySchedules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalAvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                Weekday = table.Column<int>(type: "integer", nullable: false),
                EntryTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                ExitTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                LunchStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                LunchEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProfessionalDailySchedules", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProfessionalDailySchedules_ProfessionalAvailabilities_CompanyId_ProfessionalAvailabilityId",
                    columns: x => new { x.CompanyId, x.ProfessionalAvailabilityId },
                    principalTable: "ProfessionalAvailabilities",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_ProfessionalDailySchedules_CompanyId_AvailabilityId_Weekday",
            table: "ProfessionalDailySchedules",
            columns: new[] { "CompanyId", "ProfessionalAvailabilityId", "Weekday" },
            unique: true);

        InsertLegacyDailySchedule(migrationBuilder, 1, 1);
        InsertLegacyDailySchedule(migrationBuilder, 2, 2);
        InsertLegacyDailySchedule(migrationBuilder, 4, 3);
        InsertLegacyDailySchedule(migrationBuilder, 8, 4);
        InsertLegacyDailySchedule(migrationBuilder, 16, 5);
        InsertLegacyDailySchedule(migrationBuilder, 32, 6);
        InsertLegacyDailySchedule(migrationBuilder, 64, 0);

        migrationBuilder.DropColumn(name: "WorkDays", table: "ProfessionalAvailabilities");
        migrationBuilder.DropColumn(name: "EntryTime", table: "ProfessionalAvailabilities");
        migrationBuilder.DropColumn(name: "LunchStartTime", table: "ProfessionalAvailabilities");
        migrationBuilder.DropColumn(name: "LunchEndTime", table: "ProfessionalAvailabilities");
        migrationBuilder.DropColumn(name: "ExitTime", table: "ProfessionalAvailabilities");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "WorkDays",
            table: "ProfessionalAvailabilities",
            type: "integer",
            nullable: true);
        migrationBuilder.AddColumn<TimeOnly>(
            name: "EntryTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: true);
        migrationBuilder.AddColumn<TimeOnly>(
            name: "LunchStartTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: true);
        migrationBuilder.AddColumn<TimeOnly>(
            name: "LunchEndTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: true);
        migrationBuilder.AddColumn<TimeOnly>(
            name: "ExitTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "ProfessionalDailySchedules"
                    GROUP BY "CompanyId", "ProfessionalAvailabilityId"
                    HAVING COUNT(DISTINCT ("EntryTime", "ExitTime", "LunchStartTime", "LunchEndTime")) > 1
                        OR BOOL_OR("LunchStartTime" IS NULL OR "LunchEndTime" IS NULL)
                ) THEN
                    RAISE EXCEPTION 'Cannot revert daily schedules with distinct daily hours.';
                END IF;
            END $$;
            """);

        migrationBuilder.Sql(
            """
            UPDATE "ProfessionalAvailabilities" AS availability
            SET "WorkDays" = daily_schedules."WorkDays",
                "EntryTime" = daily_schedules."EntryTime",
                "LunchStartTime" = daily_schedules."LunchStartTime",
                "LunchEndTime" = daily_schedules."LunchEndTime",
                "ExitTime" = daily_schedules."ExitTime"
            FROM (
                SELECT
                    "CompanyId",
                    "ProfessionalAvailabilityId",
                    SUM(
                        CASE "Weekday"
                            WHEN 1 THEN 1
                            WHEN 2 THEN 2
                            WHEN 3 THEN 4
                            WHEN 4 THEN 8
                            WHEN 5 THEN 16
                            WHEN 6 THEN 32
                            WHEN 0 THEN 64
                        END)::integer AS "WorkDays",
                    MIN("EntryTime") AS "EntryTime",
                    MIN("LunchStartTime") AS "LunchStartTime",
                    MIN("LunchEndTime") AS "LunchEndTime",
                    MIN("ExitTime") AS "ExitTime"
                FROM "ProfessionalDailySchedules"
                GROUP BY "CompanyId", "ProfessionalAvailabilityId"
            ) AS daily_schedules
            WHERE availability."CompanyId" = daily_schedules."CompanyId"
              AND availability."Id" = daily_schedules."ProfessionalAvailabilityId";
            """);

        migrationBuilder.AlterColumn<int>(
            name: "WorkDays",
            table: "ProfessionalAvailabilities",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
        migrationBuilder.AlterColumn<TimeOnly>(
            name: "EntryTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: false,
            oldClrType: typeof(TimeOnly),
            oldType: "time without time zone",
            oldNullable: true);
        migrationBuilder.AlterColumn<TimeOnly>(
            name: "LunchStartTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: false,
            oldClrType: typeof(TimeOnly),
            oldType: "time without time zone",
            oldNullable: true);
        migrationBuilder.AlterColumn<TimeOnly>(
            name: "LunchEndTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: false,
            oldClrType: typeof(TimeOnly),
            oldType: "time without time zone",
            oldNullable: true);
        migrationBuilder.AlterColumn<TimeOnly>(
            name: "ExitTime",
            table: "ProfessionalAvailabilities",
            type: "time without time zone",
            nullable: false,
            oldClrType: typeof(TimeOnly),
            oldType: "time without time zone",
            oldNullable: true);

        migrationBuilder.DropTable(name: "ProfessionalDailySchedules");
    }

    private static void InsertLegacyDailySchedule(MigrationBuilder migrationBuilder, int workDayFlag, int weekday) =>
        migrationBuilder.Sql(
            $"""
            INSERT INTO "ProfessionalDailySchedules"
                ("Id", "CompanyId", "ProfessionalAvailabilityId", "Weekday", "EntryTime", "ExitTime", "LunchStartTime", "LunchEndTime")
            SELECT
                md5(random()::text || clock_timestamp()::text || "Id"::text || '{workDayFlag}')::uuid,
                "CompanyId",
                "Id",
                {weekday},
                "EntryTime",
                "ExitTime",
                "LunchStartTime",
                "LunchEndTime"
            FROM "ProfessionalAvailabilities"
            WHERE ("WorkDays" & {workDayFlag}) = {workDayFlag};
            """);
}
