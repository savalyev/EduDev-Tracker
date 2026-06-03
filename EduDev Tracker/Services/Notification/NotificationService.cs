using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using EduDev_Tracker.Data.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Notification
{
    public class NotificationService: INotificationService
    {
        public async Task RequestPermissionAsync()
        {
            await LocalNotificationCenter.Current.RequestNotificationPermission();
        }

        public async Task ScheduleHabitNotificationAsync(Habit habit, HabitSchedule schedule)
        {
            await CancelHabitNotificationAsync(habit.Id);

            if (string.IsNullOrEmpty(schedule.TimeOfDay)) return;

            if (!TimeSpan.TryParse(schedule.TimeOfDay, out var reminderTime)) return;

            var activeDays = GetActiveDays(schedule.DayMask);

            foreach(var dayOfWeek in activeDays)
            {
                int notificationId = habit.Id * 10 + (int)dayOfWeek;

                var nextTrigger = GetNextOccurrence(dayOfWeek, reminderTime);

                var notification = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title = habit.Title,
                    Description = string.IsNullOrEmpty(habit.Description)
                        ? "Пора выполнить привычку!"
                        : habit.Description,
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = nextTrigger,
                        RepeatType = NotificationRepeat.Weekly,
                    }
                };

                await LocalNotificationCenter.Current.Show(notification);
            }
        }

        public Task CancelHabitNotificationAsync(int habitId)
        {
            for (int day = 0; day < 7; day++)
            {
                int notificationId = habitId * 10 + day;
                LocalNotificationCenter.Current.Cancel(notificationId);
            }
            return Task.CompletedTask;
        }

        private List<DayOfWeek> GetActiveDays(int mask)
        {
            var days = new List<DayOfWeek>();
            if ((mask & 1) != 0) days.Add(DayOfWeek.Monday);
            if ((mask & 2) != 0) days.Add(DayOfWeek.Tuesday);
            if ((mask & 4) != 0) days.Add(DayOfWeek.Wednesday);
            if ((mask & 8) != 0) days.Add(DayOfWeek.Thursday);
            if ((mask & 16) != 0) days.Add(DayOfWeek.Friday);
            if ((mask & 32) != 0) days.Add(DayOfWeek.Saturday);
            if ((mask & 64) != 0) days.Add(DayOfWeek.Sunday);
            return days;
        }

        private DateTime GetNextOccurrence(DayOfWeek targetDay, TimeSpan time)
        {
            var now = DateTime.Now;
            int daysUntil = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

            if (daysUntil == 0 && now.TimeOfDay >= time)
                daysUntil = 7;

            return now.Date.AddDays(daysUntil).Add(time);
        }
    }
}
