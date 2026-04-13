using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PennantMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    IsFinals = table.Column<bool>(type: "bit", nullable: false),
                    Division = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pool = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Round = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HomeClub = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AwayClub = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpponentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerClub = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpponentClub = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerWon = table.Column<bool>(type: "bit", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PennantMatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PennantMatches_PlayerName",
                table: "PennantMatches",
                column: "PlayerName");

            migrationBuilder.CreateIndex(
                name: "IX_PennantMatches_Year_IsFinals",
                table: "PennantMatches",
                columns: new[] { "Year", "IsFinals" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PennantMatches");
        }
    }
}
