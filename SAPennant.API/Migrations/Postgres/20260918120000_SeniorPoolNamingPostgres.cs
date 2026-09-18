using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.Postgres;

/// Senior pools are stored as "Senior Div N" so they read unambiguously
/// alongside "Junior Div 2", "Men's A2" etc. in the pool list. Golfbox itself
/// uses "Division N" or "Div N" depending on the season; the sync normalises
/// at ingest (see PennantPools.Normalise) and this brings existing rows across.
public partial class SeniorPoolNamingPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""PennantMatches"" SET ""Pool"" = 'Senior ' || ""Pool"" WHERE ""IsSenior"" AND ""Pool"" ~ '^Div [0-9]+$';
UPDATE ""RoundStatuses""  SET ""Pool"" = 'Senior ' || ""Pool"" WHERE ""Pool"" ~ '^Div [0-9]+$';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Senior ' || ""Pool"" WHERE ""Pool"" ~ '^Div [0-9]+$';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""PennantMatches"" SET ""Pool"" = substring(""Pool"" from 8) WHERE ""IsSenior"" AND ""Pool"" ~ '^Senior Div [0-9]+$';
UPDATE ""RoundStatuses""  SET ""Pool"" = substring(""Pool"" from 8) WHERE ""Pool"" ~ '^Senior Div [0-9]+$';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = substring(""Pool"" from 8) WHERE ""Pool"" ~ '^Senior Div [0-9]+$';");
    }
}
