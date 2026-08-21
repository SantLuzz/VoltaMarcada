using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWorkOrderModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_CompanyId_ProfessionalId",
                table: "WorkOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CompanyId_ProfessionalId",
                table: "WorkOrders",
                columns: new[] { "CompanyId", "ProfessionalId" });
        }
    }
}
