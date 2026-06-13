using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Pomodoro.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Pomodoro
{
    public class PomodoroService: IPomodoroService
    {
        private readonly PomodoroRepository _repo;

        public PomodoroService(PomodoroRepository repo)
        {
            _repo = repo;
        }
        public Task<List<PomodoroPreset>> GetPresetsAsync(int profileId)
            => _repo.GetPresetsAsync(profileId);
        public async Task SavePresetAsync(PomodoroPreset preset)
            => await _repo.SavePresetAsync(preset);

        public Task DeletePresetAsync(int presetId)
            => _repo.DeletePresetAsync(presetId);

        public async Task SaveSessionAsync(PomodoroSession session)
            => await _repo.SaveSessionAsync(session);

        public Task<int> CountCompletedAsync(int profileId, DateTime from, DateTime to)
            => _repo.CountCompletedAsync(profileId, from, to);

        public Task<List<DailyPomodoroStat>> GetDailyStatsAsync(int profileId, DateTime from, DateTime to)
            => _repo.GetDailyStatsAsync(profileId, from, to);
    }
}
