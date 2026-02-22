namespace iLearning.Web.Models.ViewModels.Account
{
    public class AccountIndexVm
    {
        public string? OwnedQuery { get; set; }
        public string OwnedSort { get; set; } = "created";
        public string OwnedDir { get; set; } = "desc";

        public string? AccessQuery { get; set; }
        public string AccessSort { get; set; } = "created";
        public string AccessDir { get; set; } = "desc";

        public List<InventoryRowVm> Owned { get; set; } = new();
        public List<AccessInventoryRowVm> Access { get; set; } = new();
    }

    public class InventoryRowVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public bool IsPublic { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AccessInventoryRowVm : InventoryRowVm
    {
        public string OwnerName { get; set; } = "";
    }
}
