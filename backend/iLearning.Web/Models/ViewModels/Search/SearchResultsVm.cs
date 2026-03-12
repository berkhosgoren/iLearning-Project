namespace iLearning.Web.Models.ViewModels.Search
{
    public class SearchResultsVm
    {
        public string Q { get; set; } = "";

        public List<SearchInventoryRowVm> Inventories { get; set; } = new();
        public List<SearchItemRowVm> Items { get; set; } = new();

        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class SearchInventoryRowVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public string CategoryName { get; set; } = "";
        public string CreatorName { get; set; } = "";
        public bool IsPublic { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class SearchItemRowVm
    {
        public Guid InventoryId { get; set; }
        public Guid ItemId { get; set; }

        public string InventoryTitle { get; set; } = "";
        public string CustomId { get; set; } = "";
        public string Title { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
