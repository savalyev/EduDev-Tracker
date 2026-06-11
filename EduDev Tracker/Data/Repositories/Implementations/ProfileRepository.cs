using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class ProfileRepository: BaseRepository<Profile>
    {
        public ProfileRepository(DatabaseService db) : base(db) { }

        public Task<Profile?> FindByEmailAsync(string email) =>
               Connection.Table<Profile>()
              .Where(p => p.Email == email)
              .FirstOrDefaultAsync();

        public Task<Profile?> GetLocalProfileAsync() =>
               Connection.Table<Profile>()
              .Where(p => p.IsLocal)
              .FirstOrDefaultAsync();

        public Task<Profile?> GetActiveProfileAsync() =>
               Connection.Table<Profile>()
              .Where(p => p.IsActive)
              .FirstOrDefaultAsync();

        public async Task DeactivateAllAsync()
        {
            var active = await Connection.Table<Profile>()
                                         .Where(p => p.IsActive)
                                         .ToListAsync();
            foreach (var p in active)
            {
                p.IsActive = false;
                await Connection.UpdateAsync(p);
            }
        }
    }
}
