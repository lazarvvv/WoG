using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WoG.Characters.Services.Api.Migrations
{
    /// <inheritdoc />
    public partial class EntityForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SumOfItemStats = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseSpells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpellType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseSpells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseStamina = table.Column<int>(type: "int", nullable: false),
                    BaseStrength = table.Column<int>(type: "int", nullable: false),
                    BaseAgility = table.Column<int>(type: "int", nullable: false),
                    BaseIntelligence = table.Column<int>(type: "int", nullable: false),
                    BaseFaith = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CharacterClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    BonusStamina = table.Column<int>(type: "int", nullable: false),
                    BonusStrength = table.Column<int>(type: "int", nullable: false),
                    BonusAgility = table.Column<int>(type: "int", nullable: false),
                    BonusIntelligence = table.Column<int>(type: "int", nullable: false),
                    BonusFaith = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_CharacterClasses_CharacterClassId",
                        column: x => x.CharacterClassId,
                        principalTable: "CharacterClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BonusStamina = table.Column<int>(type: "int", nullable: false),
                    BonusStrength = table.Column<int>(type: "int", nullable: false),
                    BonusAgility = table.Column<int>(type: "int", nullable: false),
                    BonusIntelligence = table.Column<int>(type: "int", nullable: false),
                    BonusFaith = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_BaseItems_BaseItemId",
                        column: x => x.BaseItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_Characters_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseSpellId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseDamage = table.Column<int>(type: "int", nullable: false),
                    BaseHealing = table.Column<int>(type: "int", nullable: false),
                    ManaRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Spells_BaseSpells_BaseSpellId",
                        column: x => x.BaseSpellId,
                        principalTable: "BaseSpells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Spells_Characters_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Name", "SumOfItemStats" },
                values: new object[,]
                {
                    { new Guid("d30962de-89e1-4a25-92cc-ccffcb202966"), "Longbow", 18 },
                    { new Guid("d8aaedf4-6441-4550-a834-6f7d4d0c4132"), "Greatsword", 20 },
                    { new Guid("f4a27ee9-bab7-4834-a331-fbced1d48c2f"), "Greatstaff", 21 },
                    { new Guid("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7"), "Shank", 11 }
                });

            migrationBuilder.InsertData(
                table: "BaseSpells",
                columns: new[] { "Id", "Description", "Name", "SpellType" },
                values: new object[,]
                {
                    { new Guid("8b71f08d-37ce-4a2a-a6a0-5a55310c68a8"), "Plays a combination of notes on their magic lute, healing the injuries of allies.", "Flash Heal", 1 },
                    { new Guid("ea725616-19f5-4ccf-b006-b1fac18097f8"), "Cries out with a supersonic voice, dealing damage to foes.", "Banshee Wail", 0 }
                });

            migrationBuilder.InsertData(
                table: "CharacterClasses",
                columns: new[] { "Id", "BaseAgility", "BaseFaith", "BaseIntelligence", "BaseStamina", "BaseStrength", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("472ac03d-2d43-49fb-8570-dd37140f2a23"), 2, 10, 5, 5, 2, "Talks a lot", "Bard" },
                    { new Guid("8534c080-e4a1-40bc-94fe-0277d5ace982"), 5, 3, 1, 7, 10, "Is big", "Warrior" },
                    { new Guid("cbd26795-0868-4eaf-8fea-7c8bb2bd7d60"), 11, 1, 4, 4, 3, "Is sneaky", "Rogue" },
                    { new Guid("d7cb583e-72d3-43fd-9f75-e15885f1ba6a"), 2, 4, 12, 4, 1, "Is smart", "Mage" }
                });

            migrationBuilder.InsertData(
                table: "Characters",
                columns: new[] { "Id", "BonusAgility", "BonusFaith", "BonusIntelligence", "BonusStamina", "BonusStrength", "CharacterClassId", "CreatedBy", "Experience", "Name" },
                values: new object[,]
                {
                    { new Guid("33d77148-d9ec-4772-a4e9-55980b8154b2"), 0, 0, 0, 0, 0, new Guid("cbd26795-0868-4eaf-8fea-7c8bb2bd7d60"), new Guid("32562268-20df-4e50-9255-3869f928e789"), 0L, "bobo" },
                    { new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7"), 0, 0, 0, 0, 0, new Guid("472ac03d-2d43-49fb-8570-dd37140f2a23"), new Guid("6f3dfb62-78e4-4ba2-886e-372830746fa6"), 0L, "gorlock" },
                    { new Guid("a5452df3-43e9-4440-aad8-5f2c62eebe57"), 0, 0, 0, 0, 0, new Guid("8534c080-e4a1-40bc-94fe-0277d5ace982"), new Guid("32562268-20df-4e50-9255-3869f928e789"), 0L, "bob" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "BaseItemId", "BonusAgility", "BonusFaith", "BonusIntelligence", "BonusStamina", "BonusStrength", "OwnerId" },
                values: new object[,]
                {
                    { new Guid("27c6ba6a-4075-426f-aa14-d0e3957115a1"), new Guid("f4a27ee9-bab7-4834-a331-fbced1d48c2f"), 0, 8, 9, 4, 0, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") },
                    { new Guid("2d06227d-1d7b-4a03-9f0b-b174f8098beb"), new Guid("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7"), 5, 0, 0, 4, 2, new Guid("33d77148-d9ec-4772-a4e9-55980b8154b2") },
                    { new Guid("61ef6a6e-fa78-4ad6-8cf5-bafe973d4e7e"), new Guid("d8aaedf4-6441-4550-a834-6f7d4d0c4132"), 2, 0, 0, 7, 11, new Guid("a5452df3-43e9-4440-aad8-5f2c62eebe57") },
                    { new Guid("7dd3a0c9-08f8-4607-96b8-452cc7682a1d"), new Guid("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7"), 6, 3, 0, 1, 1, new Guid("33d77148-d9ec-4772-a4e9-55980b8154b2") }
                });

            migrationBuilder.InsertData(
                table: "Spells",
                columns: new[] { "Id", "BaseDamage", "BaseHealing", "BaseSpellId", "ManaRequired", "OwnerId" },
                values: new object[,]
                {
                    { new Guid("936110c3-81bb-45d8-bb2c-32e8aab4432f"), 0, 15, new Guid("8b71f08d-37ce-4a2a-a6a0-5a55310c68a8"), 13, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") },
                    { new Guid("f62f6116-bab0-4f90-bef3-f2137bb4019e"), 12, 0, new Guid("ea725616-19f5-4ccf-b006-b1fac18097f8"), 13, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CharacterClassId",
                table: "Characters",
                column: "CharacterClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_BaseItemId",
                table: "Items",
                column: "BaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OwnerId",
                table: "Items",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_BaseSpellId",
                table: "Spells",
                column: "BaseSpellId");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_OwnerId",
                table: "Spells",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Spells");

            migrationBuilder.DropTable(
                name: "BaseItems");

            migrationBuilder.DropTable(
                name: "BaseSpells");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "CharacterClasses");
        }
    }
}
