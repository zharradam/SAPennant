using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPennant.API.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddRoundStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoundStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Pool = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Round = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsSettled = table.Column<bool>(type: "bit", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundStatuses_Year_Pool_Round",
                table: "RoundStatuses",
                columns: new[] { "Year", "Pool", "Round" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoundStatuses");
        }
    }
}
