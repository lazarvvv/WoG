
namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class BaseItemDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public int SumOfItemStats { get; set; }
    }
}
