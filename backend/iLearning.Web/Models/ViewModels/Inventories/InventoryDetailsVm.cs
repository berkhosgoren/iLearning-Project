using System.ComponentModel.DataAnnotations;
using iLearning.Web.Models.ViewModels.Items;

namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryDetailsVm
    {
        public Guid Id { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string DescriptionHtml { get; set; } = "";

        public string? ImageUrl { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public bool IsPublic { get; set; }
        
        public bool IsAuthenticated { get; set; }

        [MaxLength(1000)]
        public string? DiscussionNewBody { get; set; }

        public List<InventoryDiscussionCommentRowVm> DiscussionComments { get; set; } = new();

        public bool CanEdit { get; set; } = false;
        public bool CanWrite { get; set; } = false;

        public string CreatorName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public List<string> Tags { get; set; } = new();

        public string ActiveTab { get; set; } = "items";

        public List<ItemRowVm> Items { get; set; } = new();

        public List<InventoryAccessRowVm> AccessUsers { get; set; } = new();

        public string? AccessAddEmail { get; set; }
        public bool AccessAddCanWrite { get; set; } = true;

        public InventoryUpsertVm? SettingsVm { get; set; }
    }

    public class InventoryAccessRowVm
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool CanWrite { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
