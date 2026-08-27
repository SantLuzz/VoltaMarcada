using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260827100000_AddCompanyBusinessHours")]
public partial class AddCompanyBusinessHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "PublicRequestIntervalMinutes", table: "Companies", type: "integer", nullable: false, defaultValue: 60);
        migrationBuilder.CreateTable(
            name: "CompanyBusinessHours",
            columns: table => new
            {
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyBusinessHours", x => new { x.CompanyId, x.DayOfWeek });
                table.ForeignKey("FK_CompanyBusinessHours_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
            });

        for (var day = 0; day < 7; day++)
        {
            migrationBuilder.Sql($"INSERT INTO \"CompanyBusinessHours\" (\"CompanyId\", \"DayOfWeek\", \"IsOpen\", \"StartTime\", \"EndTime\") SELECT \"Id\", {day}, {(day is >= 1 and <= 5 ? "TRUE" : "FALSE")}, {(day is >= 1 and <= 5 ? "TIME '08:00'" : "NULL")}, {(day is >= 1 and <= 5 ? "TIME '18:00'" : "NULL")} FROM \"Companies\";");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanyBusinessHours");
        migrationBuilder.DropColumn(name: "PublicRequestIntervalMinutes", table: "Companies");
    }
}
