namespace iLearning.Web.Models.ViewModels.Home
{
    public class HomeInventoryCardVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string CreatorName { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public string? ImageUrl { get; set; }
        public string DescriptionHtml { get; set; } = "";
        public string DescriptionPreview { get; set; } = "";

        public int ActivityCount { get; set; }
        public int ActivityScore { get; set; }
        public DateTime? LastActivityAtUtc { get; set; }
    }
}
