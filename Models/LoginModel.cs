using System.ComponentModel.DataAnnotations;

namespace BlazorWebApp.Models
{
    public class LoginModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide the user name")]
        public string? Username { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide the password")]
        public string? Password { get; set; }
    }
}
