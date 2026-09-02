using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMaintenanceOpportunityLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                table: "MaintenanceOpportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                table: "MaintenanceOpportunities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosureReason",
                table: "MaintenanceOpportunities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConversionType",
                table: "MaintenanceOpportunities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConvertedAtUtc",
                table: "MaintenanceOpportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedEntityId",
                table: "MaintenanceOpportunities",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "MaintenanceOpportunities"
                SET "Status" = 3,
                    "ConvertedAtUtc" = COALESCE("CompletedAtUtc", "BookedAtUtc", "UpdatedAtUtc")
                WHERE "Status" = 4;
                """);

            migrationBuilder.Sql("""
                UPDATE "MaintenanceOpportunities"
                SET "ClosedAtUtc" = COALESCE("DismissedAtUtc", "UpdatedAtUtc")
                WHERE "Status" = 5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "MaintenanceOpportunities");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "MaintenanceOpportunities");

            migrationBuilder.DropColumn(
                name: "ClosureReason",
                table: "MaintenanceOpportunities");

            migrationBuilder.DropColumn(
                name: "ConversionType",
                table: "MaintenanceOpportunities");

            migrationBuilder.DropColumn(
                name: "ConvertedAtUtc",
                table: "MaintenanceOpportunities");

            migrationBuilder.DropColumn(
                name: "ConvertedEntityId",
                table: "MaintenanceOpportunities");
        }
    }
}
