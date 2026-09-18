using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.Postgres;

/// The senior finalist configs were seeded as "Senior Division N", but Golfbox
/// has always named those pools "Div N" — so the finalist cutoff never applied
/// to senior ladders. Rename the keys to match the real pool names.
public partial class FixSeniorPoolFinalistKeysPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Div 1' WHERE ""Pool"" = 'Senior Division 1';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Div 2' WHERE ""Pool"" = 'Senior Division 2';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Div 3' WHERE ""Pool"" = 'Senior Division 3';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Senior Division 1' WHERE ""Pool"" = 'Div 1';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Senior Division 2' WHERE ""Pool"" = 'Div 2';
UPDATE ""PoolFinalistConfigs"" SET ""Pool"" = 'Senior Division 3' WHERE ""Pool"" = 'Div 3';");
    }
}
