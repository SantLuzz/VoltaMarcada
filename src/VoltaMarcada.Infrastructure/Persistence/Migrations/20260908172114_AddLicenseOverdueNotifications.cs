using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseOverdueNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicenseOverdueNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyLicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseOverdueNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseOverdueNotifications_CompanyLicenseId_OwnerUserId_Se~",
                table: "LicenseOverdueNotifications",
                columns: new[] { "CompanyLicenseId", "OwnerUserId", "SentOn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseOverdueNotifications");
        }
    }
}
