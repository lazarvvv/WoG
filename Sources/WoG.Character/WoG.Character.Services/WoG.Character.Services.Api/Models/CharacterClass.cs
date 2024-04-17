using System.ComponentModel.DataAnnotations;

namespace WoG.Characters.Services.Api.Models
{
    public class CharacterClass()
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string Name { get; set; }
        [Required] public required string Description { get; set; }
        [Required] public int BaseStamina { get; set; }
        [Required] public int BaseStrength { get; set; }
        [Required] public int BaseAgility { get; set; }
        [Required] public int BaseIntelligence { get; set; }
        [Required] public int BaseFaith { get; set; }
    }
}
