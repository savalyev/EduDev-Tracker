using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Pomodoro.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class PomodoroRepository: BaseRepository<PomodoroSession>
    {
        public PomodoroRepository(DatabaseService db) : base(db)
        {
        }

        public Task<List<PomodoroPreset>> GetPresetsAsync(int profileId)
            => Connection.Table<PomodoroPreset>()
            .Where(p => p.ProfileId == profileId)
            .OrderByDescending(p => p.IsDefault)
            .ToListAsync();

        public async Task SavePresetAsync(PomodoroPreset preset)
        {
            if (preset.Id == 0)
                await Connection.InsertAsync(preset);
            else
                await Connection.UpdateAsync(preset);
        }

        public Task DeletePresetAsync(int presetId)
            => Connection.DeleteAsync<PomodoroPreset>(presetId);

        public async Task SaveSessionAsync(PomodoroSession session)
        {
            if (session.Id == 0)
                await Connection.InsertAsync(session);
            else
                await Connection.UpdateAsync(session);
        }

        // .NET ticks → unix seconds: (ticks - 621355968000000000) / 10000000
        private const string TicksToDate =
            "strftime('%Y-%m-%d', (StartedAt - 621355968000000000) / 10000000, 'unixepoch')";

        public Task<int> CountCompletedAsync(int profileId, DateTime from, DateTime to)
            => Connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM pomodoro_sessions
              WHERE ProfileId = ? AND Phase = 'Work'
              AND WasInterrupted = 0
              AND EndedAt IS NOT NULL
              AND StartedAt BETWEEN ? AND ?",
                profileId, from, to);

        public Task<List<DailyPomodoroStat>> GetDailyStatsAsync(int profileId, DateTime from, DateTime to)
            => Connection.QueryAsync<DailyPomodoroStat>(
            $@"SELECT {TicksToDate} AS Day,
                     COUNT(*) AS Sessions,
                     SUM(ActualMinutes) AS Minutes
              FROM pomodoro_sessions
              WHERE ProfileId = ? AND Phase = 'Work'
              AND WasInterrupted = 0
              AND EndedAt IS NOT NULL
              AND StartedAt BETWEEN ? AND ?
              GROUP BY {TicksToDate}
              ORDER BY Day",
                profileId, from, to);
    }
}
