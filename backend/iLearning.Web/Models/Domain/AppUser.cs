using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        public bool IsBlocked {  get; set; } = false;

        [MaxLength(500)]

        public string? PasswordHash { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Inventory> InventoriesOwned { get; set; } = new List<Inventory>();
        public ICollection<InventoryAccess> InventoryAccesses { get; set; } = new List<InventoryAccess>();

    }
}
