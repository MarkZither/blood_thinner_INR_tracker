using System;
using BloodThinnerTracker.Data.Shared;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Mobile implementation of ICurrentUserService that ensures a local user
    /// record exists and returns its internal database Id.
    /// </summary>
    public class MobileCurrentUserService : ICurrentUserService
    {
        private readonly MobileUserSeeder _seeder;
        private int? _cachedId;

        public MobileCurrentUserService(MobileUserSeeder seeder)
        {
            _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
        }

        public int? GetCurrentUserId()
        {
            if (_cachedId.HasValue)
                return _cachedId;

            try
            {
                _cachedId = _seeder.EnsureSeeded();
            }
            catch
            {
                _cachedId = null;
            }

            return _cachedId;
        }
    }
}

