using System;
using System.Linq;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Ensures a local, device-scoped user exists in the mobile SQLite DB.
    /// This provides a stable internal `Id` so local medical entities can satisfy
    /// the non-nullable `UserId` FK without requiring server-side private ids.
    /// </summary>
    public class MobileUserSeeder
    {
        private readonly ApplicationDbContext _db;

        public MobileUserSeeder(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Ensure a local user record exists and return its internal Id.
        /// Synchronous by design so it can be used from sync startup flows.
        /// </summary>
        public int EnsureSeeded()
        {
            // Look for an existing local user marker (AuthProvider == "Local" and a known email)
            var user = _db.Users.FirstOrDefault(u => u.AuthProvider == "Local" && u.Email == "mobile@local");
            if (user != null)
                return user.Id;

            user = new User
            {
                PublicId = Guid.NewGuid(),
                Email = "mobile@local",
                Name = "Local Device User",
                AuthProvider = "Local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return user.Id;
        }
    }
}
