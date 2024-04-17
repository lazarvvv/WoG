using System.ComponentModel.DataAnnotations;
using WoG.Combat.Services.Api.Enums;

namespace WoG.Combat.Services.Api.Models.Dtos
{
    public class DuelDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChallengerId { get; set; }
        public Guid DefenderId { get; set; }

        public ICollection<DuelEventDto> Events { get; set; } = [];

        public DuelOutcome DuelOutcome { get; set; }
    }
}
