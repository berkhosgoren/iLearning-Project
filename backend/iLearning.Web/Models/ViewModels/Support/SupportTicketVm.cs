using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Support
{
    public class SupportTicketVm
    {
        [Display(Name = "Support.Summary")]
        [Required(ErrorMessage = "Validation.Required")]
        [MaxLength(500)]
        public string Summary { get; set; } = string.Empty;

        [Display(Name = "Support.Priority")]
        [Required(ErrorMessage = "Validation.Required")]
        public string Priority { get; set; } = "Average";

        public string ReturnUrl { get; set; } = "/";
        public Guid? InventoryId { get; set; }

        public string ReportedByName { get; set; } = string.Empty;
        public string ReportedByEmail { get; set; } = string.Empty;
        public string? InventoryTitle { get; set; }
        public string CurrentPageUrl { get; set; } = string.Empty;
        public string AdminEmailCsv { get; set; } = string.Empty;
    }
}
