namespace iLearning.Web.Models.Domain
{
    public class UserRole
    {
        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
