using System.ComponentModel.DataAnnotations;

namespace WoG.Accounts.Services.Api.Models.Dtos
{
    public class RegisterDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
