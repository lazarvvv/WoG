namespace WoG.Combat.Services.Api.Models.Dtos
{
    public class DuelSpellDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public required string Name { get; set; }
        public required int ManaCost { get; set; }
        public required int CooldownInSeconds { get; set; }
        public int Healing { get; set; }
        public int Damage { get; set; }
    }
}
