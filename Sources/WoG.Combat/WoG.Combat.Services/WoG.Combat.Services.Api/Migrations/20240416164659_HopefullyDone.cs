using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WoG.Combat.Services.Api.Migrations
{
    /// <inheritdoc />
    public partial class HopefullyDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ticks",
                table: "Duels");

            migrationBuilder.CreateTable(
                name: "DuelEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DuelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    TimeWhenNextActionOfTypeAvailable = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialHealth = table.Column<int>(type: "int", nullable: false),
                    InitialMana = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    Healing = table.Column<int>(type: "int", nullable: false),
                    ManaSpent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuelEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuelEvent_Duels_DuelId",
                        column: x => x.DuelId,
                        principalTable: "Duels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuelEvent_DuelId",
                table: "DuelEvent",
                column: "DuelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuelEvent");

            migrationBuilder.AddColumn<long>(
                name: "Ticks",
                table: "Duels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
