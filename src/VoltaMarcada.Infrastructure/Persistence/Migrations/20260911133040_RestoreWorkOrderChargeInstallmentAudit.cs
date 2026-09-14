using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreWorkOrderChargeInstallmentAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfirmationType",
                table: "WorkOrderChargeInstallments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "WorkOrderChargeInstallments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "WorkOrderChargeInstallments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "WorkOrderChargeInstallments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "WorkOrderChargeInstallments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationType",
                table: "WorkOrderChargeInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "WorkOrderChargeInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "WorkOrderChargeInstallments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "WorkOrderChargeInstallments");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "WorkOrderChargeInstallments");
        }
    }
}
