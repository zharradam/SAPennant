using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SAPennant.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "HonourRoll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Competition = table.Column<string>(type: "text", nullable: false),
                    Pool = table.Column<string>(type: "text", nullable: false),
                    Winner = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HonourRoll", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HonourRollNarratives",
                columns: table => new
                {
                    Competition = table.Column<string>(type: "text", nullable: false),
                    Narrative = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HonourRollNarratives", x => x.Competition);
                });

            migrationBuilder.CreateTable(
                name: "PennantMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    IsFinals = table.Column<bool>(type: "boolean", nullable: false),
                    Division = table.Column<string>(type: "text", nullable: false),
                    Pool = table.Column<string>(type: "text", nullable: false),
                    Round = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    HomeClub = table.Column<string>(type: "text", nullable: false),
                    AwayClub = table.Column<string>(type: "text", nullable: false),
                    PlayerName = table.Column<string>(type: "text", nullable: false),
                    OpponentName = table.Column<string>(type: "text", nullable: false),
                    PlayerClub = table.Column<string>(type: "text", nullable: false),
                    OpponentClub = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    PlayerWon = table.Column<bool>(type: "boolean", nullable: true),
                    Format = table.Column<string>(type: "text", nullable: false),
                    PlayerHandicap = table.Column<string>(type: "text", nullable: true),
                    OpponentHandicap = table.Column<string>(type: "text", nullable: true),
                    Venue = table.Column<string>(type: "text", nullable: true),
                    IsSenior = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PennantMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoundStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Pool = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Round = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Year = table.Column<int>(type: "integer", nullable: false),
                    RegularId = table.Column<int>(type: "integer", nullable: false),
                    FinalsId = table.Column<int>(type: "integer", nullable: true),
                    SeniorRegularId = table.Column<int>(type: "integer", nullable: true),
                    SeniorFinalsId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PennantMatches_PlayerName",
                table: "PennantMatches",
                column: "PlayerName");

            migrationBuilder.CreateIndex(
                name: "IX_PennantMatches_Year_IsFinals",
                table: "PennantMatches",
                columns: new[] { "Year", "IsFinals" });

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
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "HonourRoll");

            migrationBuilder.DropTable(
                name: "HonourRollNarratives");

            migrationBuilder.DropTable(
                name: "PennantMatches");

            migrationBuilder.DropTable(
                name: "RoundStatuses");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "SyncLogs");
        }
    }
}
