using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WoG.Characters.Services.Api.Migrations
{
    /// <inheritdoc />
    public partial class HopefullyDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("27c6ba6a-4075-426f-aa14-d0e3957115a1"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("2d06227d-1d7b-4a03-9f0b-b174f8098beb"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("61ef6a6e-fa78-4ad6-8cf5-bafe973d4e7e"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("7dd3a0c9-08f8-4607-96b8-452cc7682a1d"));

            migrationBuilder.DeleteData(
                table: "Spells",
                keyColumn: "Id",
                keyValue: new Guid("936110c3-81bb-45d8-bb2c-32e8aab4432f"));

            migrationBuilder.DeleteData(
                table: "Spells",
                keyColumn: "Id",
                keyValue: new Guid("f62f6116-bab0-4f90-bef3-f2137bb4019e"));

            migrationBuilder.DropColumn(
                name: "SpellType",
                table: "BaseSpells");

            migrationBuilder.AddColumn<int>(
                name: "CooldownInSeconds",
                table: "Spells",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "BaseItemId", "BonusAgility", "BonusFaith", "BonusIntelligence", "BonusStamina", "BonusStrength", "OwnerId" },
                values: new object[,]
                {
                    { new Guid("0eedb8bf-8a5b-4cfd-86bd-84244a33d317"), new Guid("d8aaedf4-6441-4550-a834-6f7d4d0c4132"), 2, 0, 0, 7, 11, new Guid("a5452df3-43e9-4440-aad8-5f2c62eebe57") },
                    { new Guid("2738d6db-a78f-4634-bd22-2f2b9838cff1"), new Guid("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7"), 5, 0, 0, 4, 2, new Guid("33d77148-d9ec-4772-a4e9-55980b8154b2") },
                    { new Guid("2b4d5870-47a5-4bdf-89de-b3b18b747d88"), new Guid("f4a27ee9-bab7-4834-a331-fbced1d48c2f"), 0, 8, 9, 4, 0, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") },
                    { new Guid("facd47b4-50a5-4e1a-a46d-71a6f24b53b3"), new Guid("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7"), 6, 3, 0, 1, 1, new Guid("33d77148-d9ec-4772-a4e9-55980b8154b2") }
                });

            migrationBuilder.InsertData(
                table: "Spells",
                columns: new[] { "Id", "BaseDamage", "BaseHealing", "BaseSpellId", "CooldownInSeconds", "ManaRequired", "OwnerId" },
                values: new object[,]
                {
                    { new Guid("1ceee048-508b-4963-931c-10a430b353d1"), 0, 15, new Guid("8b71f08d-37ce-4a2a-a6a0-5a55310c68a8"), 5, 13, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") },
                    { new Guid("5b1d6866-2c6c-4b69-b248-24e3a1d415ae"), 12, 0, new Guid("ea725616-19f5-4ccf-b006-b1fac18097f8"), 5, 13, new Guid("a0b6969f-e3ff-4723-9427-5f10909c16d7") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("0eedb8bf-8a5b-4cfd-86bd-84244a33d317"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("2738d6db-a78f-4634-bd22-2f2b9838cff1"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("2b4d5870-47a5-4bdf-89de-b3b18b747d88"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: new Guid("facd47b4-50a5-4e1a-a46d-71a6f24b53b3"));

            migrationBuilder.DeleteData(
                table: "Spells",
                keyColumn: "Id",
                keyValue: new Guid("1ceee048-508b-4963-931c-10a430b353d1"));

            migrationBuilder.DeleteData(
                table: "Spells",
                keyColumn: "Id",
                keyValue: new Guid("5b1d6866-2c6c-4b69-b248-24e3a1d415ae"));

            migrationBuilder.DropColumn(
                name: "CooldownInSeconds",
                table: "Spells");

            migrationBuilder.AddColumn<int>(
                name: "SpellType",
                table: "BaseSpells",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "BaseSpells",
                keyColumn: "Id",
                keyValue: new Guid("8b71f08d-37ce-4a2a-a6a0-5a55310c68a8"),
                column: "SpellType",
                value: 1);

            migrationBuilder.UpdateData(
                table: "BaseSpells",
                keyColumn: "Id",
                keyValue: new Guid("ea725616-19f5-4ccf-b006-b1fac18097f8"),
                column: "SpellType",
                value: 0);

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
        }
    }
}
