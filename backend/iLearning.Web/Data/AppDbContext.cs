using iLearning.Web.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryAccess> InventoryAccesses => Set<InventoryAccess>();

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<InventoryTag> InventoryTags => Set<InventoryTag>();

        public DbSet<Item> Items => Set<Item>();
        public DbSet<ItemLike> ItemLikes => Set<ItemLike>();
        public DbSet<ItemComment> ItemComments => Set<ItemComment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Users
            modelBuilder.Entity<AppUser>()
                .HasIndex(x => x.Email)
                .IsUnique();

            //Roles
            modelBuilder.Entity<Role>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(x => new { x.UserId, x.RoleId });

            //Categories
            modelBuilder.Entity<Category>()
                .HasIndex(x => x.Name)
                .IsUnique();

            //Inventory Version (optimistic locking)
            modelBuilder.Entity<Inventory>()
                .Property(x => x.Version)
                .IsConcurrencyToken();

            //Item Version (optimistic locking)
            modelBuilder.Entity<Item>()
                .Property(x => x.Version)
                .IsConcurrencyToken();

            //InventoryAccess
            modelBuilder.Entity<InventoryAccess>()
                .HasKey(x => new { x.InventoryId, x.UserId });

            //Tags
            modelBuilder.Entity<Tag>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<InventoryTag>()
                .HasKey(x => new { x.InventoryId, x.TagId });

            //Custom item id unique per inventory (DB enforced)
            modelBuilder.Entity<Item>()
                .HasIndex(x => new { x.InventoryId, x.CustomId })
                .IsUnique();

            //Item likes: one like per user per item
            modelBuilder.Entity<ItemLike>()
                .HasKey(x => new { x.ItemId, x.UserId });

            // Cascades avoiding loops letting DB handle integrity
            modelBuilder.Entity<Inventory>()
                .HasMany(x => x.Items)
                .WithOne(x => x.Inventory)
                .HasForeignKey(x => x.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Item>()
                .HasMany(x => x.Comments)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Item>()
                .HasMany(x => x.Likes)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            //Seed categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Equipment" },
                new Category { Id = 2, Name = "Furniture" },
                new Category { Id = 3, Name = "Book" },
                new Category { Id = 4, Name = "Other" }
                );

            //Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" }
                );
        }
    }
}
