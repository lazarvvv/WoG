using System.ComponentModel.DataAnnotations;
using WoG.Characters.Services.Api.Enums;

namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class SpellDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid OwnerId { get; set; }
        public required BaseSpellDto BaseSpell { get; set; }
        public required int CooldownInSeconds { get; set; }
        public int BaseDamage { get; set; }
        public int BaseHealing { get; set; }
        public int ManaRequired { get; set; }

        public string Name => this.BaseSpell.Name;
        public string Description => this.BaseSpell.Description;
    }
}
