namespace iLearning.Web.Models.ViewModels.Home
{
    public class HomeInventoryCardVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string CategoryName { get; set; } = "Other";
        public string CreatorName { get; set; } = "Unknown";
        public DateTime CreatedAtUtc { get; set; }
        public string? ImageUrl { get; set; }
        public string DescriptionHtml { get; set; } = "";

        public int ActivityCount { get; set; }
        public DateTime? LastActivityAtUtc { get; set; }
    }
}
