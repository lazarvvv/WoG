using System.ComponentModel.DataAnnotations;
using WoG.Combat.Services.Api.Enums;

namespace WoG.Combat.Services.Api.Models
{
    public class DuelEvent
    {
        [Key] public Guid Id { get; set; }
        [Required] public long Sequence { get; set; }
        [Required] public Guid CharacterId { get; set; }
        public Guid DuelId { get; set; }
        public EventType EventType { get; set; }
        public DateTime TimeWhenNextActionOfTypeAvailable { get; set; }
        public int InitialHealth { get; set; }
        public int InitialMana { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaSpent { get; set; }
    }
}
