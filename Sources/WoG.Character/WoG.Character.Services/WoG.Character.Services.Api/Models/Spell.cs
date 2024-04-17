using System.ComponentModel.DataAnnotations;
using WoG.Characters.Services.Api.Enums;

namespace WoG.Characters.Services.Api.Models
{
    public class Spell
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required Guid OwnerId { get; set; }
        [Required] public required BaseSpell BaseSpell { get; set; }

        [Required] public required int CooldownInSeconds { get; set; }
        public int BaseDamage { get; set; }
        public int BaseHealing { get; set; }
        public int ManaRequired { get; set; }
    }
}
