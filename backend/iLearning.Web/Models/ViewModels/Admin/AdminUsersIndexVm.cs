using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Admin
{
    public class AdminUsersIndexVm
    {
        public string? Q { get; set; }

        public string Sort { get; set; } = "created";
        public string Dir { get; set; } = "desc";

        public List<AdminUserRowVm> Users { get; set; } = new();

        public string? Message { get; set; }
    }

    public class AdminUserRowVm
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        public bool IsBlocked { get; set; }
        public bool IsAdmin { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public int OwnedInventoriesCount { get; set; }

    }
}
