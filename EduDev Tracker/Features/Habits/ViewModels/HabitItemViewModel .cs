using CommunityToolkit.Mvvm.ComponentModel;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Habits;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitItemViewModel : ObservableObject
    {
        private readonly Habit _habit;
        private HabitSchedule _habitSchedule;
        private readonly Func<int, bool, string, Task>? _onToggled;
        private bool _suppressCallback;

        public HabitItemViewModel(
            Habit habit, 
            HabitSchedule habitSchedule, 
            Func<int, bool, string, Task>? onToggled = null)
        {
            _habit = habit;
            _habitSchedule = habitSchedule;
            _onToggled = onToggled;
        }

        public int HabitId => _habit.Id;
        public string TitleHabit => _habit.Title;
        public string Description => _habit.Description;
        public string Icon => _habit.Icon ?? "default_icon.png";
        public bool IsActive => !_habit.IsArchived && !_habit.IsFrozen;
        public string Schedule => ParseDaysMask(_habitSchedule.DayMask);
        public string TimeOfDay => _habitSchedule.TimeOfDay;
        public string Type => _habit.TypeString;
        public double TodayProress => 0; //временнйы костыль
        public string TodayProgressText => ParseProgress();
        public string StreakText => "-";

        [ObservableProperty]
        private bool isCompleted;

        partial void OnIsCompletedChanged(bool value)
        {
            if (_suppressCallback) return;
            _onToggled?.Invoke(HabitId, value, Type);
        }

        public void SetCompletedSilently(bool value)
        {
            _suppressCallback = true;
            IsCompleted = value;
            _suppressCallback = false;
        }


        private string ParseDaysMask(int mask)
        {
            var days = new List<string>();

            if ((mask & 1) != 0) days.Add("Пн");
            if ((mask & 2) != 0) days.Add("Вт");
            if ((mask & 4) != 0) days.Add("Ср");
            if ((mask & 8) != 0) days.Add("Чт");
            if ((mask & 16) != 0) days.Add("Пт");
            if ((mask & 32) != 0) days.Add("Сб");
            if ((mask & 64) != 0) days.Add("Вс");

            return string.Join(", ", days);
        }

        private int CountDays(int mask)
        {
            int count = 0;

            if ((mask & 1) != 0) count++;
            if ((mask & 2) != 0) count++;
            if ((mask & 4) != 0) count++;
            if ((mask & 8) != 0) count++;
            if ((mask & 16) != 0) count++;
            if ((mask & 32) != 0) count++;
            if ((mask & 64) != 0) count++;

            return count;
        }

        private string ParseProgress()
        {

            if (_habitSchedule.DayMask == 127)
            {
                return $"Сегодня: 0/{_habit.TargetValue} {_habit.TargetUnit}";
            }
            else
            {
                return $"На этой неделе: 0/{CountDays(_habitSchedule.DayMask)} сессий";
            }
        }

    }
}
