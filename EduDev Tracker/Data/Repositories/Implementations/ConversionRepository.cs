using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class ConversionRepository: BaseRepository<ConversionHistory>
    {
        public ConversionRepository(DatabaseService db) : base(db) { }

        public Task<List<ConversionHistory>> GetRecentAsync(int profileId, int limit = 50)
            => Connection.QueryAsync<ConversionHistory>(
            @"SELECT * FROM conversion_history
              WHERE ProfileId = ?
              ORDER BY CreatedAt DESC
              LIMIT ?",
                profileId, limit);

        public Task<List<ConversionHistory>> GetByTypeAsync(
            int profileId, ConversionType type, int limit = 20)
            => Connection.QueryAsync<ConversionHistory>(
            @"SELECT * FROM conversion_history
              WHERE ProfileId = ? AND Type = ?
              ORDER BY CreatedAt DESC
              LIMIT ?",
            profileId, type.ToString(), limit);

        public Task AddAsync(ConversionHistory entry)
            => Connection.InsertAsync(entry);

        public Task ClearTodayAsync(int profileId)
        {
            var startOfDay = DateTime.Today.ToUniversalTime();
            return Connection.ExecuteAsync(
                "DELETE FROM conversion_history WHERE ProfileId = ? AND CreatedAt >= ?",
                profileId, startOfDay);
        }

        public Task ClearAllAsync(int profileId)
            => Connection.ExecuteAsync(
            "DELETE FROM conversion_history WHERE ProfileId = ?", profileId);
    }
}
