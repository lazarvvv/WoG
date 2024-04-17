using System.ComponentModel.DataAnnotations;

namespace WoG.Characters.Services.Api.Models
{
    public class Item
    {
        [Key] public Guid Id { get; set; }
        [Required] public required Guid OwnerId { get; set; }
        [Required] public required BaseItem BaseItem { get; set; }
        
        public int BonusStamina { get; set; }
        public int BonusStrength { get; set; }
        public int BonusAgility { get; set; }
        public int BonusIntelligence { get; set; }
        public int BonusFaith { get; set; }
    }
}
