using WoG.Combat.Services.Api.Enums;

namespace WoG.Combat.Services.Api.Models.Dtos
{
    public class DuelEventDto
    {
        public long Sequence { get; set; }
        public Guid DuelId { get; set; }
        public Guid CharacterId { get; set; }
        public EventType EventType { get; set; }
        public DateTime TimeWhenNextActionOfTypeAvailable { get; set; }
        public int InitialHealth { get; set; }
        public int InitialMana { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaSpent { get; set; }
    }
}
