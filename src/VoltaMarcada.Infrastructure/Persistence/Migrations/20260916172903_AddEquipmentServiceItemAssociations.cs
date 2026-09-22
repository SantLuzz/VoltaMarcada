using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentServiceItemAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "WorkOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "WorkOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "QuoteItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "QuoteItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "WorkOrderItems" AS item
                SET "EquipmentId" = work_order."EquipmentId",
                    "ServiceId" = work_order."ServiceId"
                FROM "WorkOrders" AS work_order
                WHERE item."CompanyId" = work_order."CompanyId"
                  AND item."WorkOrderId" = work_order."Id"
                  AND item."Type" = 1;
                """);

            migrationBuilder.Sql("""
                UPDATE "QuoteItems" AS item
                SET "EquipmentId" = quote."EquipmentId",
                    "ServiceId" = quote."PrimaryServiceId"
                FROM "Quotes" AS quote
                WHERE item."CompanyId" = quote."CompanyId"
                  AND item."QuoteId" = quote."Id"
                  AND item."Type" = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_CompanyId_EquipmentId",
                table: "WorkOrderItems",
                columns: new[] { "CompanyId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_CompanyId_ServiceId",
                table: "WorkOrderItems",
                columns: new[] { "CompanyId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_CompanyId_EquipmentId",
                table: "QuoteItems",
                columns: new[] { "CompanyId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_CompanyId_ServiceId",
                table: "QuoteItems",
                columns: new[] { "CompanyId", "ServiceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Equipment_CompanyId_EquipmentId",
                table: "QuoteItems",
                columns: new[] { "CompanyId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Services_CompanyId_ServiceId",
                table: "QuoteItems",
                columns: new[] { "CompanyId", "ServiceId" },
                principalTable: "Services",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderItems_Equipment_CompanyId_EquipmentId",
                table: "WorkOrderItems",
                columns: new[] { "CompanyId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderItems_Services_CompanyId_ServiceId",
                table: "WorkOrderItems",
                columns: new[] { "CompanyId", "ServiceId" },
                principalTable: "Services",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Equipment_CompanyId_EquipmentId",
                table: "QuoteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Services_CompanyId_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderItems_Equipment_CompanyId_EquipmentId",
                table: "WorkOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderItems_Services_CompanyId_ServiceId",
                table: "WorkOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderItems_CompanyId_EquipmentId",
                table: "WorkOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderItems_CompanyId_ServiceId",
                table: "WorkOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_CompanyId_EquipmentId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_CompanyId_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "WorkOrderItems");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "WorkOrderItems");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "QuoteItems");
        }
    }
}
