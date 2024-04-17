using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace WoG.Characters.Services.Api.Models
{
    public class Character
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string Name { get; set; }
        [Required] public required CharacterClass CharacterClass { get; set; }
        [Required] public Guid CreatedBy { get; set; }

        public long Experience { get; set; }
        public int BonusStamina { get; set; }
        public int BonusStrength { get; set; }
        public int BonusAgility { get; set; }
        public int BonusIntelligence { get; set; }
        public int BonusFaith { get; set; }
        public ICollection<Item> Items { get; set; } = [];
        public ICollection<Spell> Spells { get; set; } = [];
    }
}
