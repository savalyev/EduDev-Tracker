using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Pomodoro.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Pomodoro
{
    public interface IPomodoroService
    {
        public Task<List<PomodoroPreset>> GetPresetsAsync(int profileId);
        public Task SavePresetAsync(PomodoroPreset preset);
        Task DeletePresetAsync(int presetId);
        public Task SaveSessionAsync(PomodoroSession session);
        Task<int> CountCompletedAsync(int profileId, DateTime from, DateTime to);
        Task<List<DailyPomodoroStat>> GetDailyStatsAsync(int profileId, DateTime from, DateTime to);
    }
}
