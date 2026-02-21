using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryDetailsVm
    {
        public Guid Id { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        public bool CanEdit { get; set; }
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }

        public string CreatorName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public List<string> Tags { get; set; } = new();

        public string ActiveTab { get; set; } = "items";
    }
}
