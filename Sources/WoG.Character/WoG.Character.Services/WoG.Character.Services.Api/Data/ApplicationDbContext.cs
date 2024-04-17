using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using WoG.Characters.Services.Api.Constants;
using WoG.Characters.Services.Api.Enums;
using WoG.Characters.Services.Api.Models;
using CharacterConstants = WoG.Characters.Services.Api.Constants.SeedingConstants.Characters;
using CharacterClassConstants = WoG.Characters.Services.Api.Constants.SeedingConstants.CharacterClasses;
using BaseItemConstants = WoG.Characters.Services.Api.Constants.SeedingConstants.BaseItems;
using AccountConstants = WoG.Characters.Services.Api.Constants.SeedingConstants.Accounts;
using BaseSpellConstants = WoG.Characters.Services.Api.Constants.SeedingConstants.BaseSpells;

namespace WoG.Characters.Services.Api.Data
{
    public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<CharacterClass> CharacterClasses { get; set; }
        public DbSet<BaseItem> BaseItems { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<BaseSpell> BaseSpells { get; set; }
        public DbSet<Spell> Spells { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.BaseItem)
                .WithMany();

            modelBuilder.Entity<Spell>()
                .HasOne(i => i.BaseSpell)
                .WithMany();

            modelBuilder.Entity<Character>()
                .HasOne(c => c.CharacterClass)
                .WithMany();

            modelBuilder.Entity<Character>()
                .HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.OwnerId);

            modelBuilder.Entity<Character>()
                .HasMany(c => c.Spells)
                .WithOne()
                .HasForeignKey(i => i.OwnerId);

            SeedCharacterClass(modelBuilder, CharacterClassConstants.Ids.Warrior, "Warrior", "Is big", 7, 10, 5, 1, 3);
            SeedCharacterClass(modelBuilder, CharacterClassConstants.Ids.Rogue, "Rogue", "Is sneaky", 4, 3, 11, 4, 1);
            SeedCharacterClass(modelBuilder, CharacterClassConstants.Ids.Bard, "Bard", "Talks a lot", 5, 2, 2, 5, 10);
            SeedCharacterClass(modelBuilder, CharacterClassConstants.Ids.Mage, "Mage", "Is smart", 4, 1, 2, 12, 4);

            SeedCharacter(modelBuilder, CharacterConstants.Ids.Bob, CharacterConstants.Names.Bob,
                CharacterClassConstants.Ids.Warrior, AccountConstants.Ids.Bob);
            SeedCharacter(modelBuilder, CharacterConstants.Ids.Gorlock, CharacterConstants.Names.Gorlock,
                CharacterClassConstants.Ids.Bard, AccountConstants.Ids.Karen);
            SeedCharacter(modelBuilder, CharacterConstants.Ids.BobRogue, CharacterConstants.Names.BobRogue,
                CharacterClassConstants.Ids.Rogue, AccountConstants.Ids.Bob);

            SeedBaseItem(modelBuilder, BaseItemConstants.Ids.Greatsword, BaseItemConstants.Names.Greatsword, BaseItemConstants.Stats.Greatsword);
            SeedBaseItem(modelBuilder, BaseItemConstants.Ids.Longbow, BaseItemConstants.Names.Longbow, BaseItemConstants.Stats.Longbow);
            SeedBaseItem(modelBuilder, BaseItemConstants.Ids.Shank, BaseItemConstants.Names.Shank, BaseItemConstants.Stats.Shank);
            SeedBaseItem(modelBuilder, BaseItemConstants.Ids.Greatstaff, BaseItemConstants.Names.Greatstaff, BaseItemConstants.Stats.Greatstaff);

            SeedItem(modelBuilder, Guid.NewGuid(), BaseItemConstants.Ids.Greatsword, CharacterConstants.Ids.Bob, 7, 11, 2, 0, 0);
            SeedItem(modelBuilder, Guid.NewGuid(), BaseItemConstants.Ids.Shank, CharacterConstants.Ids.BobRogue, 1, 1, 6, 0, 3);
            SeedItem(modelBuilder, Guid.NewGuid(), BaseItemConstants.Ids.Shank, CharacterConstants.Ids.BobRogue, 4, 2, 5, 0, 0);
            SeedItem(modelBuilder, Guid.NewGuid(), BaseItemConstants.Ids.Greatstaff, CharacterConstants.Ids.Gorlock, 4, 0, 0, 9, 8);

            SeedBaseSpell(modelBuilder, BaseSpellConstants.Ids.BansheeWail, BaseSpellConstants.Names.BansheeWail,
                BaseSpellConstants.Descriptions.BansheeWail, SpellType.Damage);
            SeedBaseSpell(modelBuilder, BaseSpellConstants.Ids.SongOfSerenity, BaseSpellConstants.Names.SongOfSerenity,
                BaseSpellConstants.Descriptions.SongOfSerenity, SpellType.HealingSpell);
            
            SeedSpell(modelBuilder, Guid.NewGuid(), BaseSpellConstants.Ids.BansheeWail, CharacterConstants.Ids.Gorlock, 13, 12, 0, 5);
            SeedSpell(modelBuilder, Guid.NewGuid(), BaseSpellConstants.Ids.SongOfSerenity, CharacterConstants.Ids.Gorlock, 13, 0, 15, 5);        
        }

        private static void SeedCharacter(ModelBuilder modelBuilder, Guid characterId,
            string name, Guid characterClassId, Guid accountId)
        {
            modelBuilder.Entity<Character>()
                .HasData(new
                {
                    Id = characterId,
                    Name = name,
                    Experience = 0L,
                    BonusStamina = 0,
                    BonusStrength = 0,
                    BonusAgility = 0,
                    BonusIntelligence = 0,
                    BonusFaith = 0,
                    CharacterClassId = characterClassId,
                    CreatedBy = accountId
                });
        }

        private static void SeedBaseItem(ModelBuilder modelBuilder, Guid baseItemId,
            string name, int sumOfStats)
        {
            modelBuilder.Entity<BaseItem>()
                .HasData(new
                {
                    Id = baseItemId,
                    Name = name,
                    SumOfItemStats = sumOfStats
                });
        }

        private static void SeedItem(ModelBuilder modelBuilder, Guid itemId, Guid baseItemId, Guid ownerId,
           int bonusStamina, int bonusStrength, int bonusAgility, int bonusIntelligence, int bonusFaith)
        {
            modelBuilder.Entity<Item>()
                .HasData(new
                {
                    Id = itemId,
                    OwnerId = ownerId,
                    BaseItemId = baseItemId,
                    BonusStamina = bonusStamina,
                    BonusStrength = bonusStrength,
                    BonusAgility = bonusAgility,
                    BonusIntelligence = bonusIntelligence,
                    BonusFaith = bonusFaith
                });
        }

        private static void SeedBaseSpell(ModelBuilder modelBuilder, Guid baseItemId,
            string name, string description, SpellType spellType)
        {
            modelBuilder.Entity<BaseSpell>()
                .HasData(new
                {
                    Id = baseItemId,
                    Name = name,
                    Description = description,
                    SpellType = spellType
                });
        }

        private static void SeedSpell(ModelBuilder modelBuilder, Guid spellId, Guid baseSpellId, Guid ownerId,
            int manaRequired, int baseDamage, int baseHealing, int cooldownInSeconds)
        {
            modelBuilder.Entity<Spell>()
                .HasData(new
                {
                    Id = spellId,
                    BaseSpellId = baseSpellId,
                    OwnerId = ownerId,
                    ManaRequired = manaRequired,
                    BaseDamage = baseDamage,
                    BaseHealing = baseHealing,
                    CooldownInSeconds = cooldownInSeconds
                });
        }

        private static void SeedCharacterClass(ModelBuilder modelBuilder, Guid classId, string name, string description,
            int baseStamina, int baseStrength, int baseAgility, int baseIntelligence, int baseFaith)
        {
            modelBuilder.Entity<CharacterClass>()
                .HasData(new
                {
                    Id = classId,
                    Name = name,
                    Description = description,
                    BaseStamina = baseStamina,
                    BaseStrength = baseStrength,
                    BaseAgility = baseAgility,
                    BaseIntelligence = baseIntelligence,
                    BaseFaith = baseFaith
                });
        }
    }
}
