using CommunityToolkit.Mvvm.ComponentModel;
using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class TaskItemViewModel: ObservableObject
    {
        public int TaskId { get; init; }

        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string categoryName = string.Empty;
        [ObservableProperty] private string priorityLabel = string.Empty;
        [ObservableProperty] private string priorityBgColor = "#182333";
        [ObservableProperty] private string priorityBorderColor = "#4A5A6A";
        [ObservableProperty] private string deadlineText = string.Empty;
        [ObservableProperty] private string deadlineBgColor = "#182333";
        [ObservableProperty] private string deadlineTextColor = "#99FFFFFF";
        [ObservableProperty] private bool isCompleted;
        [ObservableProperty] private string statusKey = "Todo";
        [ObservableProperty] private bool hasReminder;
        [ObservableProperty] private string reminderLabel = "";

        public static TaskItemViewModel FromModel(TaskItem task)
        {
            try {
                var isOverdue = task.DueAt.HasValue && task.DueAt.Value < DateTime.Now && task.Status != Data.Models.TaskStatus.Done;
                var isToday = task.DueAt.HasValue && task.DueAt.Value.Date == DateTime.Today;

                return new TaskItemViewModel
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    CategoryName = task.Category ?? "Без категории",
                    PriorityLabel = GetPriorityLabel(task.Priority),
                    PriorityBgColor = GetPriorityBg(task.Priority),
                    PriorityBorderColor = GetPriorityBorder(task.Priority),
                    DeadlineText = FormatDeadline(task.DueAt),
                    DeadlineBgColor = isOverdue ? "#2A1010" : "#182333",
                    DeadlineTextColor = isOverdue ? "#FF6B6B" : isToday ? "#FFDD44" : "#99FFFFFF",
                    IsCompleted   = task.Status == Data.Models.TaskStatus.Done,
                    StatusKey     = task.Status.ToString(),
                    HasReminder   = task.ReminderAt.HasValue && task.ReminderAt.Value > DateTime.Now,
                    ReminderLabel = task.ReminderAt.HasValue && task.ReminderAt.Value > DateTime.Now
                                        ? FormatReminder(task.ReminderAt.Value)
                                        : ""
                };
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FromModel ERROR] TaskId={task.Id} | {ex.Message}");
                throw;
            }
            }
        private static string GetPriorityLabel(TaskPriority p) => p switch
        {
            TaskPriority.Urgent => "Критичный",
            TaskPriority.High => "Высокий",
            TaskPriority.Medium => "Средний",
            TaskPriority.Low => "Низкий",
            _ => "—"
        };

        private static string GetPriorityBg(TaskPriority p) => p switch
        {
            TaskPriority.Urgent => "#2A0A0A",
            TaskPriority.High => "#2A1800",
            TaskPriority.Medium => "#0A1A2A",
            TaskPriority.Low => "#0A2010",
            _ => "#182333"
        };

        private static string GetPriorityBorder(TaskPriority p) => p switch
        {
            TaskPriority.Urgent => "#C0392B",
            TaskPriority.High => "#E67E22",
            TaskPriority.Medium => "#2980B9",
            TaskPriority.Low => "#27AE60",
            _ => "#4A5A6A"
        };

        private static string FormatReminder(DateTime remind)
        {
            if (remind.Date == DateTime.Today)         return "Сегодня " + remind.ToString("HH:mm");
            if (remind.Date == DateTime.Today.AddDays(1)) return "Завтра " + remind.ToString("HH:mm");
            return remind.ToString("d MMM HH:mm");
        }

        private static string FormatDeadline(DateTime? due)
        {
            if (due is null) return "Без срока";
            if (due.Value.Date == DateTime.Today) return "Сегодня " + due.Value.ToString("HH:mm");
            if (due.Value.Date == DateTime.Today.AddDays(1)) return "Завтра " + due.Value.ToString("HH:mm");
            if (due.Value < DateTime.Now) return due.Value.ToString("d MMM") + " (просрочено)";
            return due.Value.ToString("d MMM HH:mm");
        }
    }
}
