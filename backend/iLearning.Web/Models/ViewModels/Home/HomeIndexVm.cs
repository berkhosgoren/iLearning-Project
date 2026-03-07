namespace iLearning.Web.Models.ViewModels.Home
{
    public class HomeIndexVm
    {
        public List<HomeInventoryCardVm> LatestInventories { get; set; } = new();
        public List<HomeInventoryCardVm> PopularInventories { get; set; } = new();
    }
}
