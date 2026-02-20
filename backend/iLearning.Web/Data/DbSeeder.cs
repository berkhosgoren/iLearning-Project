using iLearning.Web.Models.Domain;
using iLearning.Web.Security;
using Microsoft.EntityFrameworkCore;


namespace iLearning.Web.Data
{
    public class DbSeeder
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public DbSeeder(AppDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        public async Task SeedAsync()
        {
            var adminEmail = _cfg["SeedAdmin:Email"] ?? "admin@ilearning.local";
            var adminName = _cfg["SeedAdmin:Name"] ?? "Admin";
            var adminPassword = _cfg["SeedAdmin:Password"] ?? "Admin123!";

            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole is null) return;

            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (admin is null)
            {
                admin = new AppUser
                {
                    Name = adminName,
                    Email = adminEmail,
                    PasswordHash = PasswordHasher.Hash(adminPassword),
                    IsBlocked = false
                };

                _db.Users.Add(admin);
                await _db.SaveChangesAsync();
            }

            var hasRole = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id);

            if (!hasRole)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = adminRole.Id,
                });

                await _db.SaveChangesAsync();
            }
        }
    }
}
