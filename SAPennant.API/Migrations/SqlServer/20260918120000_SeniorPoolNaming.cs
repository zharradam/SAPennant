using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer;

/// Senior pools are stored as "Senior Div N" so they read unambiguously
/// alongside "Junior Div 2", "Men's A2" etc. in the pool list. Golfbox itself
/// uses "Division N" or "Div N" depending on the season; the sync normalises
/// at ingest (see PennantPools.Normalise) and this brings existing rows across.
public partial class SeniorPoolNaming : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE PennantMatches SET Pool = 'Senior ' + Pool WHERE IsSenior = 1 AND Pool LIKE 'Div [0-9]%';
UPDATE RoundStatuses   SET Pool = 'Senior ' + Pool WHERE Pool LIKE 'Div [0-9]%';
UPDATE PoolFinalistConfigs SET Pool = 'Senior ' + Pool WHERE Pool LIKE 'Div [0-9]%';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE PennantMatches SET Pool = STUFF(Pool, 1, 7, '') WHERE IsSenior = 1 AND Pool LIKE 'Senior Div [0-9]%';
UPDATE RoundStatuses   SET Pool = STUFF(Pool, 1, 7, '') WHERE Pool LIKE 'Senior Div [0-9]%';
UPDATE PoolFinalistConfigs SET Pool = STUFF(Pool, 1, 7, '') WHERE Pool LIKE 'Senior Div [0-9]%';");
    }
}
