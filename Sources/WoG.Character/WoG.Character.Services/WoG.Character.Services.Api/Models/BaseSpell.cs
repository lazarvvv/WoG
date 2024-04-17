using System.ComponentModel.DataAnnotations;
using WoG.Characters.Services.Api.Enums;

namespace WoG.Characters.Services.Api.Models
{
    public class BaseSpell
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string Name { get; set; }
        [Required] public required string Description { get; set; }
    }
}
