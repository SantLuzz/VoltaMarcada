using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

public partial class AddCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Document = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                WhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Customers_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CustomerAddresses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                AddressType = table.Column<int>(type: "integer", nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PostalCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Complement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                StateId = table.Column<Guid>(type: "uuid", nullable: false),
                CityId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerAddresses_Cities_CityId",
                    column: x => x.CityId,
                    principalTable: "Cities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CustomerAddresses_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CustomerAddresses_States_StateId",
                    column: x => x.StateId,
                    principalTable: "States",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_CompanyId_Name",
            table: "Customers",
            columns: new[] { "CompanyId", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerAddresses_CityId",
            table: "CustomerAddresses",
            column: "CityId");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerAddresses_CustomerId",
            table: "CustomerAddresses",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerAddresses_StateId",
            table: "CustomerAddresses",
            column: "StateId");

        migrationBuilder.CreateIndex(
            name: "UX_CustomerAddresses_CustomerId_Primary",
            table: "CustomerAddresses",
            columns: new[] { "CustomerId", "IsPrimary" },
            unique: true,
            filter: "\"IsPrimary\" = true");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerAddresses");

        migrationBuilder.DropTable(
            name: "Customers");
    }
}
