using EduDev_Tracker.Data.Models;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class ReminderRepository : BaseRepository<Reminder>
    {
        public ReminderRepository(DatabaseService db) : base(db) { }

        public Task<List<Reminder>> GetAllByProfileAsync(int profileId)
            => Connection.Table<Reminder>()
                .Where(r => r.ProfileId == profileId && !r.IsFired)
                .ToListAsync();

        public Task<Reminder?> GetByEntityAsync(string entityType, int entityId)
            => Connection.Table<Reminder>()
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .FirstOrDefaultAsync();

        public Task DeleteByEntityAsync(string entityType, int entityId)
            => Connection.ExecuteAsync(
                "DELETE FROM reminders WHERE EntityType=? AND EntityId=?",
                entityType, entityId);
    }
}
