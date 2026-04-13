using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer;

public partial class AddPoolFinalistConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PoolFinalistConfigs",
            columns: table => new
            {
                Pool = table.Column<string>(maxLength: 50, nullable: false),
                FinalistCount = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PoolFinalistConfigs", x => x.Pool);
            });

        migrationBuilder.Sql(@"INSERT INTO PoolFinalistConfigs (Pool, FinalistCount) VALUES
('Simpson Cup', 4),
('Bonnar Cup', 4),
('Men''s A2', 2),
('Men''s B2', 4),
('Sanderson Cup', 3),
('Pike Cup', 3),
('Women''s A3', 3),
('Women''s A4', 3),
('Women''s Cleek 1', 4),
('Women''s Cleek 2', 2),
('Sharp Cup', 2),
('Junior Division 2', 4),
('Junior Division 3', 3),
('Senior Division 1', 4),
('Senior Division 2', 4),
('Senior Division 3', 4)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PoolFinalistConfigs");
    }
}