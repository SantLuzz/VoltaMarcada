using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

public partial class AddServiceRequestsAndQuotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProblemDescription", table: "AppointmentRequests", type: "character varying(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<int>(name: "Type", table: "Appointments", type: "integer", nullable: false, defaultValue: 2);
        migrationBuilder.AddColumn<string>(name: "Description", table: "Appointments", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "DiscountAmount", table: "WorkOrders", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<Guid>(name: "SourceQuoteId", table: "WorkOrders", type: "uuid", nullable: true);
        migrationBuilder.CreateTable(name: "Quotes", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), CustomerId = table.Column<Guid>(type: "uuid", nullable: false), CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false), EquipmentId = table.Column<Guid>(type: "uuid", nullable: false), PrimaryServiceId = table.Column<Guid>(type: "uuid", nullable: false), ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: true), GeneratedWorkOrderId = table.Column<Guid>(type: "uuid", nullable: true), Status = table.Column<int>(type: "integer", nullable: false), DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false), Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true), ValidUntil = table.Column<DateOnly>(type: "date", nullable: false), PublicTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true), PublicTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), PublicTokenRevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Quotes", x => x.Id);
            table.ForeignKey(name: "FK_Quotes_Companies_CompanyId", column: x => x.CompanyId, principalTable: "Companies", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable(name: "QuoteItems", columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), QuoteId = table.Column<Guid>(type: "uuid", nullable: false), Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false), Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false), UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false), Type = table.Column<int>(type: "integer", nullable: false), DisplayOrder = table.Column<int>(type: "integer", nullable: false) }, constraints: table => { table.PrimaryKey("PK_QuoteItems", x => x.Id); table.ForeignKey(name: "FK_QuoteItems_Quotes_QuoteId", column: x => x.QuoteId, principalTable: "Quotes", principalColumn: "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "ServiceRequests", columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), CompanyId = table.Column<Guid>(type: "uuid", nullable: false), CustomerId = table.Column<Guid>(type: "uuid", nullable: false), AppointmentRequestId = table.Column<Guid>(type: "uuid", nullable: false), QuoteId = table.Column<Guid>(type: "uuid", nullable: true), WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true), ProblemDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true), Status = table.Column<int>(type: "integer", nullable: false), CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => table.PrimaryKey("PK_ServiceRequests", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Quotes_PublicTokenHash", table: "Quotes", column: "PublicTokenHash", unique: true, filter: "\"PublicTokenHash\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_WorkOrders_SourceQuoteId", table: "WorkOrders", column: "SourceQuoteId", unique: true, filter: "\"SourceQuoteId\" IS NOT NULL");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QuoteItems"); migrationBuilder.DropTable(name: "Quotes"); migrationBuilder.DropTable(name: "ServiceRequests"); migrationBuilder.DropColumn(name: "ProblemDescription", table: "AppointmentRequests"); migrationBuilder.DropColumn(name: "Type", table: "Appointments"); migrationBuilder.DropColumn(name: "Description", table: "Appointments"); migrationBuilder.DropColumn(name: "DiscountAmount", table: "WorkOrders"); migrationBuilder.DropColumn(name: "SourceQuoteId", table: "WorkOrders");
    }
}
