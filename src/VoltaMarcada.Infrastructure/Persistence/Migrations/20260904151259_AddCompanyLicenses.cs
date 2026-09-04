using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TrialStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrialEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyLicenses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CompanyLicenses_CompanyId",
                table: "CompanyLicenses",
                column: "CompanyId",
                unique: true);
            migrationBuilder.Sql("INSERT INTO \"CompanyLicenses\" (\"Id\", \"CompanyId\", \"Status\", \"TrialStartedAtUtc\", \"TrialEndsAtUtc\", \"PaidUntilUtc\", \"CreatedAtUtc\", \"UpdatedAtUtc\") SELECT md5(\"Id\"::text)::uuid, \"Id\", 0, \"CreatedAtUtc\", \"CreatedAtUtc\" + INTERVAL '14 days', NULL, \"CreatedAtUtc\", \"UpdatedAtUtc\" FROM \"Companies\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyLicenses");
        }
    }
}
