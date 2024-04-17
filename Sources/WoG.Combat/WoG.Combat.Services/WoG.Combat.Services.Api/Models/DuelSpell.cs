using System.ComponentModel.DataAnnotations;

namespace WoG.Combat.Services.Api.Models
{
    public class DuelSpell
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid OwnerId { get; set; }
        [Required] public required string Name { get; set; }
        [Required] public required int ManaCost { get; set; }
        [Required] public required int CooldownInSeconds { get; set; }
        public int Healing { get; set; }
        public int Damage { get; set; }

    }
}
