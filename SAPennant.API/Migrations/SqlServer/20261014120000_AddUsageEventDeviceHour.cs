using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer;

/// Hand-written and applied as SQL, like the other recent migrations.
public partial class AddUsageEventDeviceHour : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Hour", table: "UsageEvents", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>(name: "IsMobile", table: "UsageEvents", type: "bit", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Hour", table: "UsageEvents");
        migrationBuilder.DropColumn(name: "IsMobile", table: "UsageEvents");
    }
}
