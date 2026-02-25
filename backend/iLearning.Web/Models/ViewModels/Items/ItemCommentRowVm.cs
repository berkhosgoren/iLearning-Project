namespace iLearning.Web.Models.ViewModels.Items
{
    public class ItemCommentRowVm
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string UserName { get; set; } = "";

        public string Body { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; }

        public bool CanDelete { get; set; }
    }
}
