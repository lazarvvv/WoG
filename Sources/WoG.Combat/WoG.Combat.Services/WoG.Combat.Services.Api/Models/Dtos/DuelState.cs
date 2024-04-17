using System.ComponentModel.DataAnnotations;
using WoG.Combat.Services.Api.Enums;

namespace WoG.Combat.Services.Api.Models.Dtos
{
    public class DuelState
    {
        public required Guid Id { get; set; }
        public required CharacterInfo Challenger { get; set; }
        public required CharacterInfo Defender { get; set; }

        public DuelOutcome DuelOutcome { get; set; }
        public DateTime DuelStart { get; set; } = DateTime.Now;
    }

    public class CharacterInfo
    {
        public Guid Id { get; set; }
        public int InitialHealth { get; set; }
        public int Health { get; set; }
        public int InitialMana { get; set; }
        public int Mana { get; set; }

        public int Damage { get; set; }
        public int Healing { get; set; }

        public DateTime DamageCooldown { get; set; }
        public DateTime HealingCooldown { get; set; }

        public Dictionary<Guid, DateTime> SpellCooldowns { get; set; } = [];
    }
}
