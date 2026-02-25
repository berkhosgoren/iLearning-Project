using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Items
{
    public class ItemDetailsVm
    {
        public Guid InventoryId { get; set; }
        public Guid ItemId { get; set; }

        public string InventoryTitle { get; set; } = "";

        public string CustomId { get; set; } = "";
        public string Title { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public string CreatedByName { get; set; } = "";
        public string? UpdatedByName { get; set; }

        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }

        public bool CanWrite { get; set; }

        public bool IsAuthenticated { get; set; }
        public bool IsLikedByMe { get; set; }

        [MaxLength(1000)]
        public string? NewCommentBody { get; set; }

        public List<ItemCommentRowVm> Comments { get; set; } = new();

        public string? String1 { get; set; }
        public string? String2 { get; set; }
        public string? String3 { get; set; }

        public string? Text1 { get; set; }
        public string? Text2 { get; set; }
        public string? Text3 { get; set; }

        public decimal? Number1 { get; set; }
        public decimal? Number2 { get; set; }
        public decimal? Number3 { get; set; }

        public bool? Bool1 { get; set; }
        public bool? Bool2 { get; set; }
        public bool? Bool3 { get; set; }

        public string? Link1 { get; set; }
        public string? Link2 { get; set; }
        public string? Link3 { get; set; }
    }
}
