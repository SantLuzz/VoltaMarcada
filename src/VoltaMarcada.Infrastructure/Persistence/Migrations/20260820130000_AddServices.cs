using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820130000_AddServices")]
public partial class AddServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Services",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                PriceType = table.Column<int>(type: "integer", nullable: false),
                BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                AllowOnlineBooking = table.Column<bool>(type: "boolean", nullable: false),
                RequireBeforePhotos = table.Column<bool>(type: "boolean", nullable: false),
                RequireAfterPhotos = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Services", x => x.Id);
                table.UniqueConstraint("AK_Services_CompanyId_Id", x => new { x.CompanyId, x.Id });
                table.ForeignKey(
                    name: "FK_Services_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ServiceChecklistItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServiceChecklistItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_ServiceChecklistItems_Services_CompanyId_ServiceId",
                    columns: x => new { x.CompanyId, x.ServiceId },
                    principalTable: "Services",
                    principalColumns: new[] { "CompanyId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Services_CompanyId_Name",
            table: "Services",
            columns: new[] { "CompanyId", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_ServiceChecklistItems_CompanyId_ServiceId_DisplayOrder",
            table: "ServiceChecklistItems",
            columns: new[] { "CompanyId", "ServiceId", "DisplayOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ServiceChecklistItems");
        migrationBuilder.DropTable(name: "Services");
    }
}
