using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SAPennant.API.Migrations.Postgres;

/// Hand-written like the other Postgres migrations and applied as SQL on Neon
/// (the Postgres snapshot has drifted, so the differ can't generate one).
public partial class AddUsageEventsPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsageEvents",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Day = table.Column<DateOnly>(type: "date", nullable: false),
                Event = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                VisitorId = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                SessionId = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsageEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UsageEvents_Day",
            table: "UsageEvents",
            column: "Day");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UsageEvents");
    }
}
