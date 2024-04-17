using System.ComponentModel.DataAnnotations;

namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class CharacterClassDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int BaseStamina { get; set; }
        public int BaseStrength { get; set; }
        public int BaseAgility { get; set; }
        public int BaseIntelligence { get; set; }
        public int BaseFaith { get; set; }
    }
}
