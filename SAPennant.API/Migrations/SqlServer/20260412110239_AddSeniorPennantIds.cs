using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddSeniorPennantIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeniorFinalsId",
                table: "Seasons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeniorRegularId",
                table: "Seasons",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE Seasons SET SeniorRegularId = 1472, SeniorFinalsId = 1509 WHERE Year = 2021");
            migrationBuilder.Sql("UPDATE Seasons SET SeniorRegularId = 1697, SeniorFinalsId = 1735 WHERE Year = 2022");
            migrationBuilder.Sql("UPDATE Seasons SET SeniorRegularId = 1793, SeniorFinalsId = 1970 WHERE Year = 2023");
            migrationBuilder.Sql("UPDATE Seasons SET SeniorRegularId = 1996, SeniorFinalsId = 1997 WHERE Year = 2024");
            migrationBuilder.Sql("UPDATE Seasons SET SeniorRegularId = 2149 WHERE Year = 2025");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeniorFinalsId",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "SeniorRegularId",
                table: "Seasons");
        }
    }
}
