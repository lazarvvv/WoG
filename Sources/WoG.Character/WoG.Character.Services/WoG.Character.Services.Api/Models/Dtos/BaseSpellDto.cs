using System.ComponentModel.DataAnnotations;
using WoG.Characters.Services.Api.Enums;

namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class BaseSpellDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}
