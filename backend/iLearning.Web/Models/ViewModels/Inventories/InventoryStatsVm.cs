using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryStatsVm
    {
        public Guid InventoryId { get; set; }

        public int ItemsTotal { get; set; }
        public int LikesTotal { get; set; }
        public int CommentsTotal { get; set; }

        public DateTime? LastItemCreatedAtUtc { get; set; }
        public DateTime? LastItemUpdatedAtUtc { get; set; }

        public List<TopItemVm> TopItemsByLikes { get; set; } = new();
        public List<TopItemVm> TopItemsByComments { get; set; } = new();
    }

    public class TopItemVm
    {
        public Guid ItemId { get; set; }

        public string CustomId { get; set; } = "";

        public string Title { get; set; } = "";

        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
    }
}
