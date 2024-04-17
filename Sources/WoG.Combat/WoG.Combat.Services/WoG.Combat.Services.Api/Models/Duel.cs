using System.ComponentModel.DataAnnotations;
using WoG.Combat.Services.Api.Enums;
using WoG.Combat.Services.Api.Models.Dtos;
namespace WoG.Combat.Services.Api.Models
{
    public class Duel
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public Guid ChallengerId { get; set; }
        [Required] public Guid DefenderId { get; set; }


        public ICollection<DuelEvent> Events { get; set; } = [];
        public ICollection<DuelSpell> LegalSpells { get; set; } = [];
        public DuelOutcome DuelOutcome { get; set; }
        
    }
}
