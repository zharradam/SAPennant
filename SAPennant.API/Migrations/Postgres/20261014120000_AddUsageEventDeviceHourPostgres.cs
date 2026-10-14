using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.Postgres;

/// Hand-written and applied as SQL on Neon, like the other Postgres migrations.
public partial class AddUsageEventDeviceHourPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Hour", table: "UsageEvents", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>(name: "IsMobile", table: "UsageEvents", type: "boolean", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Hour", table: "UsageEvents");
        migrationBuilder.DropColumn(name: "IsMobile", table: "UsageEvents");
    }
}
