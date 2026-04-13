using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddHandicapAndVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpponentHandicap",
                table: "PennantMatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerHandicap",
                table: "PennantMatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Venue",
                table: "PennantMatches",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpponentHandicap",
                table: "PennantMatches");

            migrationBuilder.DropColumn(
                name: "PlayerHandicap",
                table: "PennantMatches");

            migrationBuilder.DropColumn(
                name: "Venue",
                table: "PennantMatches");
        }
    }
}
