namespace WoG.Combat.Services.Api.Models.Dtos
{
    public class DuelRequestDto
    {
        public Guid ChallengerId { get; set; }
        public Guid DefenderId { get; set; }
    }
}
