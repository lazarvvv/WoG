using System.ComponentModel.DataAnnotations;

namespace WoG.Accounts.Services.Api.Models.Dtos
{
    public class LoginDto
    {
        [Required]
        [DataType(DataType.Text)] 
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
