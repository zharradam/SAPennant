using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations
{
    /// <inheritdoc />
    public partial class IsSenior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSenior",
                table: "PennantMatches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSenior",
                table: "PennantMatches");
        }
    }
}
