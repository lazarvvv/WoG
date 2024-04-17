using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WoG.Combat.Services.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedEnumInsteadOfBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuelOutcome",
                table: "Duels",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuelOutcome",
                table: "Duels");
        }
    }
}
