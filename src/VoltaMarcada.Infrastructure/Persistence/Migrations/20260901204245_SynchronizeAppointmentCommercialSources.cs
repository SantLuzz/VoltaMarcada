using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizeAppointmentCommercialSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceAppointmentId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAppointmentId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CompanyId_SourceAppointmentId",
                table: "WorkOrders",
                columns: new[] { "CompanyId", "SourceAppointmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CompanyId_SourceAppointmentId",
                table: "Quotes",
                columns: new[] { "CompanyId", "SourceAppointmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Appointments_CompanyId_SourceAppointmentId",
                table: "Quotes",
                columns: new[] { "CompanyId", "SourceAppointmentId" },
                principalTable: "Appointments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Appointments_CompanyId_SourceAppointmentId",
                table: "WorkOrders",
                columns: new[] { "CompanyId", "SourceAppointmentId" },
                principalTable: "Appointments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Appointments_CompanyId_SourceAppointmentId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Appointments_CompanyId_SourceAppointmentId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_CompanyId_SourceAppointmentId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_CompanyId_SourceAppointmentId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "SourceAppointmentId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SourceAppointmentId",
                table: "Quotes");
        }
    }
}
