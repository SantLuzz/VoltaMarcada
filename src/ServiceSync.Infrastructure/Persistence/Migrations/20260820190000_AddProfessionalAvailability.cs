using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceSync.Infrastructure.Persistence;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820190000_AddProfessionalAvailability")]
public partial class AddProfessionalAvailability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProfessionalAvailabilities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkDays = table.Column<int>(type: "integer", nullable: false),
                EntryTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                LunchStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                LunchEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                ExitTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProfessionalAvailabilities", x => x.Id);
                table.UniqueConstraint("AK_ProfessionalAvailabilities_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey(
                    name: "FK_ProfessionalAvailabilities_Professionals_CompanyId_ProfessionalId",
                    columns: x => new { x.CompanyId, x.ProfessionalId },
                    principalTable: "Professionals",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ScheduleBlocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfessionalId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                IsFullDay = table.Column<bool>(type: "boolean", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScheduleBlocks", x => x.Id);
                table.UniqueConstraint("AK_ScheduleBlocks_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey(
                    name: "FK_ScheduleBlocks_Professionals_CompanyId_ProfessionalId",
                    columns: x => new { x.CompanyId, x.ProfessionalId },
                    principalTable: "Professionals",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_ProfessionalAvailabilities_CompanyId_ProfessionalId",
            table: "ProfessionalAvailabilities",
            columns: new[] { "CompanyId", "ProfessionalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ScheduleBlocks_CompanyId_ProfessionalId_StartDate_EndDate",
            table: "ScheduleBlocks",
            columns: new[] { "CompanyId", "ProfessionalId", "StartDate", "EndDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ScheduleBlocks");
        migrationBuilder.DropTable(name: "ProfessionalAvailabilities");
    }
}
