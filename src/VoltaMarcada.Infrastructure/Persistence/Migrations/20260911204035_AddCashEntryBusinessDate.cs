using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashEntryBusinessDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderCharges_WorkOrders_CompanyId_WorkOrderId",
                table: "WorkOrderCharges");

            migrationBuilder.DropIndex(
                name: "UX_WorkOrderCharges_WorkOrderId",
                table: "WorkOrderCharges");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkOrderId",
                table: "WorkOrderCharges",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateOnly>(
                name: "BusinessDate",
                table: "WorkOrderCharges",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"WorkOrderCharges\" SET \"BusinessDate\" = (\"CreatedAtUtc\" AT TIME ZONE 'America/Sao_Paulo')::date WHERE \"BusinessDate\" IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "BusinessDate",
                table: "WorkOrderCharges",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "WorkOrderCharges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualEntry",
                table: "WorkOrderCharges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "WorkOrderChargeInstallments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCharges_CompanyId_BusinessDate",
                table: "WorkOrderCharges",
                columns: new[] { "CompanyId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "UX_WorkOrderCharges_WorkOrderId",
                table: "WorkOrderCharges",
                column: "WorkOrderId",
                unique: true,
                filter: "\"WorkOrderId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderCharges_WorkOrders_CompanyId_WorkOrderId",
                table: "WorkOrderCharges",
                columns: new[] { "CompanyId", "WorkOrderId" },
                principalTable: "WorkOrders",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderCharges_WorkOrders_CompanyId_WorkOrderId",
                table: "WorkOrderCharges");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderCharges_CompanyId_BusinessDate",
                table: "WorkOrderCharges");

            migrationBuilder.DropIndex(
                name: "UX_WorkOrderCharges_WorkOrderId",
                table: "WorkOrderCharges");

            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "WorkOrderCharges");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WorkOrderCharges");

            migrationBuilder.DropColumn(
                name: "IsManualEntry",
                table: "WorkOrderCharges");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkOrderId",
                table: "WorkOrderCharges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "WorkOrderChargeInstallments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_WorkOrderCharges_WorkOrderId",
                table: "WorkOrderCharges",
                column: "WorkOrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderCharges_WorkOrders_CompanyId_WorkOrderId",
                table: "WorkOrderCharges",
                columns: new[] { "CompanyId", "WorkOrderId" },
                principalTable: "WorkOrders",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
