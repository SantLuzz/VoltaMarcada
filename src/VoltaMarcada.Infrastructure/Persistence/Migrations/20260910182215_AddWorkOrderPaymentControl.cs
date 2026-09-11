using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderPaymentControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoChargeReason",
                table: "WorkOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoChargeRecordedAtUtc",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NoChargeRecordedByUserId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentControlEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PixKey",
                table: "Companies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PixKeyType",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkOrderCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderCharges_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderCharges_WorkOrders_CompanyId_WorkOrderId",
                        columns: x => new { x.CompanyId, x.WorkOrderId },
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderChargeInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherPaymentMethodDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderChargeInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderChargeInstallments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderChargeInstallments_WorkOrderCharges_WorkOrderCharg~",
                        column: x => x.WorkOrderChargeId,
                        principalTable: "WorkOrderCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChargeInstallments_CompanyId_Status_PaidAtUtc",
                table: "WorkOrderChargeInstallments",
                columns: new[] { "CompanyId", "Status", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChargeInstallments_WorkOrderChargeId",
                table: "WorkOrderChargeInstallments",
                column: "WorkOrderChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCharges_CompanyId_WorkOrderId",
                table: "WorkOrderCharges",
                columns: new[] { "CompanyId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "UX_WorkOrderCharges_WorkOrderId",
                table: "WorkOrderCharges",
                column: "WorkOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderChargeInstallments");

            migrationBuilder.DropTable(
                name: "WorkOrderCharges");

            migrationBuilder.DropColumn(
                name: "NoChargeReason",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "NoChargeRecordedAtUtc",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "NoChargeRecordedByUserId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PaymentControlEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PixKey",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PixKeyType",
                table: "Companies");
        }
    }
}
