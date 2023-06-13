using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Models
{
    public class ResetPassword
    {
        [Required]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage="The Password and ConfirmPassword does not match")]
        public string? ConfirmPassword { get; set; }
        public string? Email { get; set; }
        public string? Token { get; set; }
    }
}
