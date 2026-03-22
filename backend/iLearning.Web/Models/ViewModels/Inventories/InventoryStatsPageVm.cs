namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryStatsPageVm
    {
        public Guid InventoryId { get; set; }

        public string InventoryTitle { get; set; } = "";
        public string InventoryCategoryName { get; set; } = "";
        public string InventoryOwnerName { get; set; } = "";
        public bool IsPublic { get; set; }

        public bool CanEdit { get; set; }
        public bool CanWrite { get; set; }

        public InventoryStatsVm Stats { get; set; } = new();
    }
}
