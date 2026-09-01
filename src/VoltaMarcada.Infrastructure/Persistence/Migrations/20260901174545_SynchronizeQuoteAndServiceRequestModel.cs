using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaMarcada.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901174545_SynchronizeQuoteAndServiceRequestModel")]
public sealed class SynchronizeQuoteAndServiceRequestModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The earlier schema migration created these objects but did not update the EF model snapshot.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration only synchronizes the EF model snapshot.
    }
}
