using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Notification
{
    public interface INotificationService
    {
        Task ScheduleHabitNotificationAsync(Habit habit, HabitSchedule schedule);
        Task CancelHabitNotificationAsync(int habitId);
        Task RequestPermissionAsync();
    }
}
