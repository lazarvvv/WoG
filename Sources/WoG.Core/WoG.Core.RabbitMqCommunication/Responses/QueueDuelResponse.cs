using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoG.Core.RabbitMqCommunication.Responses
{
    public class QueueDuelResponse
    {
        public Guid DuelId { get; set; } = Guid.Empty;

        public required CharacterInfo ChallengerInfo { get; set; }
        public List<SpellInfo> ChallengerSpells { get; set; } = [];
        
        public required CharacterInfo DefenderInfo { get; set; }
        public List<SpellInfo> DefenderSpells { get; set; } = [];
    }

    public record SpellInfo(Guid Id, Guid OwnerId, string Name, int ManaCost, int CooldownInSeconds, int Healing, int Damage);
    public record CharacterInfo(Guid Id, int Damage, int Healing, int Health, int Mana);
}
