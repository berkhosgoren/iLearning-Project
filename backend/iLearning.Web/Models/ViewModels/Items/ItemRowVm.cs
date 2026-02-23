namespace iLearning.Web.Models.ViewModels.Items
{
    public class ItemRowVm
    {
        public Guid Id { get; set; }
        public string CustomId { get; set; } = "";
        public string Title { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
    }
}
