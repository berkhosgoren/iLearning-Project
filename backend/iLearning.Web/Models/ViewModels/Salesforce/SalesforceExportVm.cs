using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Salesforce
{
    public class SalesforceExportVm
    {
        [Display(Name  = "Salesforce.AccountName")]
        [Required(ErrorMessage = "Validation.Required")]
        [MaxLength(255)]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "Salesforce.FirstName")]
        [MaxLength(80)]
        public string? FirstName { get; set; }

        [Display(Name = "Salesforce.LastName")]
        [Required(ErrorMessage = "Validation.Required")]
        [MaxLength(80)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Salesforce.Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Salesforce.Phone")]
        [Phone]
        [MaxLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "Salesforce.Title")]
        [MaxLength(120)]
        public string? Title { get; set; }

        [Display(Name = "Salesforce.Website")]
        [MaxLength(255)]
        public string? Website { get; set; }

        [Display(Name = "Salesforce.Description")]
        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
