using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Auth
{
    public class RegisterVm
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(600)]
        public string Password { get; set; } = string.Empty;
    }
}
