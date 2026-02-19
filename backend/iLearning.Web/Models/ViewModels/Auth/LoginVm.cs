using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Auth
{
    public class LoginVm
    {
        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = true;
    }
}
