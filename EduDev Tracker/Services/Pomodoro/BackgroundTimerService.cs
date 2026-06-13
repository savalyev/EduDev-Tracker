using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Pomodoro
{
    public class BackgroundTimerService
    {
        private const int PomodoroNotificationId = 1001;

        public async Task ScheduleEndNotificationAsync(
        int remainingSeconds, string phaseLabel, string taskTitle)
        {
            // Запрашиваем разрешение на уведомления (Android 13+, iOS)
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var fireAt = DateTime.Now.AddSeconds(remainingSeconds);

            var body = string.IsNullOrWhiteSpace(taskTitle)
                ? "Пора переключиться!"
                : $"Задача: {taskTitle}";

            var notification = new NotificationRequest
            {
                NotificationId = PomodoroNotificationId,
                Title = $"{phaseLabel} завершена ✓",
                Description = body,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = fireAt
                },
                Android = new AndroidOptions
                {
                    ChannelId = "pomodoro_channel",
                    IconSmallName = new AndroidIcon("notification_icon"),
                    Priority = AndroidPriority.High
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
            System.Diagnostics.Debug.WriteLine(
                $"[BackgroundTimerService] Notification scheduled at {fireAt:HH:mm:ss}");
        }

        public void CancelEndNotification()
        {
            LocalNotificationCenter.Current.Cancel(PomodoroNotificationId);
        }
    }
}
