using System.ComponentModel.DataAnnotations;

namespace WoG.Characters.Services.Api.Models
{
    public class BaseItem
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public required string Name { get; set; }
        
        public int SumOfItemStats { get; set; }
    }
}
