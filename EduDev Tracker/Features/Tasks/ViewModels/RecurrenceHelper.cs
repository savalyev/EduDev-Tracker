using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class RecurrenceHelper: ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        [NotifyPropertyChangedFor(nameof(IsEditingRecurrence))]
        [NotifyPropertyChangedFor(nameof(RecurringHint))]
        private bool isRecurring;

        public bool IsEditingRecurrence => IsRecurring;
        public string RecurringHint => IsRecurring ? "Задача будет повторяться автоматически" : "Разовая задача";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWeekly))]
        [NotifyPropertyChangedFor(nameof(IsMonthly))]
        [NotifyPropertyChangedFor(nameof(IntervalLabel))]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        private string selectedRuleType = "daily";

        public bool IsWeekly => SelectedRuleType == "weekly";
        public bool IsMonthly => SelectedRuleType == "monthly";

        public string IntervalLabel => SelectedRuleType switch
        {
            "daily" => "Каждые N дней",
            "weekly" => "Каждые N недель",
            "monthly" => "Каждые N месяцев",
            _ => "Интервал"
        };

        [RelayCommand]
        private void SelectRuleType(string ruleType) => SelectedRuleType = ruleType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        private double intervalN = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        [NotifyPropertyChangedFor(nameof(MonColor))]
        [NotifyPropertyChangedFor(nameof(MonBorder))]
        [NotifyPropertyChangedFor(nameof(TueColor))]
        [NotifyPropertyChangedFor(nameof(TueBorder))]
        [NotifyPropertyChangedFor(nameof(WedColor))]
        [NotifyPropertyChangedFor(nameof(WedBorder))]
        [NotifyPropertyChangedFor(nameof(ThuColor))]
        [NotifyPropertyChangedFor(nameof(ThuBorder))]
        [NotifyPropertyChangedFor(nameof(FriColor))]
        [NotifyPropertyChangedFor(nameof(FriBorder))]
        [NotifyPropertyChangedFor(nameof(SatColor))]
        [NotifyPropertyChangedFor(nameof(SatBorder))]
        [NotifyPropertyChangedFor(nameof(SunColor))]
        [NotifyPropertyChangedFor(nameof(SunBorder))]
        private int daysMask = 0;

        [RelayCommand]
        private void ToggleDay(string bitStr)
        {
            int bit = int.Parse(bitStr);
            DaysMask ^= bit;
        }

        private bool HasDay(int bit) => (DaysMask & bit) != 0;
        private string DayBg(int bit) => HasDay(bit) ? "#5B5FEF" : "#14182E";
        private string DayBorder(int bit) => HasDay(bit) ? "#7B7FFF" : "#495061";

        public string MonColor => DayBg(1);
        public string MonBorder => DayBorder(1);
        public string TueColor => DayBg(2);
        public string TueBorder => DayBorder(2);
        public string WedColor => DayBg(4);
        public string WedBorder => DayBorder(4);
        public string ThuColor => DayBg(8);
        public string ThuBorder => DayBorder(8);
        public string FriColor => DayBg(16);
        public string FriBorder => DayBorder(16);
        public string SatColor => DayBg(32);
        public string SatBorder => DayBorder(32);
        public string SunColor => DayBg(64);
        public string SunBorder => DayBorder(64);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        private double dayOfMonth = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        private DateTime recurrenceEndDate = DateTime.Today.AddYears(1);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecurrenceSummary))]
        private bool hasEndDate = false;

        public DateTime Today => DateTime.Today;

        public string RecurrenceSummary
        {
            get
            {
                if (!IsRecurring) return "Без повторений";

                var n = (int)IntervalN;
                string text = SelectedRuleType switch
                {
                    "daily" => n == 1 ? "Каждый день" : $"Каждые {n} дня(ей)",
                    "weekly" => BuildWeeklySummary(n),
                    "monthly" => $"Каждый месяц, {(int)DayOfMonth}-го числа",
                    _ => "—"
                };
                return text;
            }
        }

        private string BuildWeeklySummary(int n)
        {
            var days = new List<string>();
            if (HasDay(1)) days.Add("Пн");
            if (HasDay(2)) days.Add("Вт");
            if (HasDay(4)) days.Add("Ср");
            if (HasDay(8)) days.Add("Чт");
            if (HasDay(16)) days.Add("Пт");
            if (HasDay(32)) days.Add("Сб");
            if (HasDay(64)) days.Add("Вс");

            var daysStr = days.Count > 0 ? string.Join(", ", days) : "не выбраны дни";
            return n == 1 ? $"Каждую неделю: {daysStr}" : $"Каждые {n} нед.: {daysStr}";
        }

        public void LoadFromRecurrence(TaskRecurrence? rec)
        {
            if (rec is null)
            {
                IsRecurring = false;
                return;
            }
            IsRecurring = true;
            SelectedRuleType = rec.RuleType;
            IntervalN = rec.IntervalN;
            DaysMask = rec.DaysMask ?? 0;
            DayOfMonth = rec.DayOfMonth ?? 1;
            HasEndDate = rec.EndDate.HasValue;
            RecurrenceEndDate = rec.EndDate ?? DateTime.Today.AddYears(1);
        }

        public TaskRecurrence? BuildRecurrence(int taskId)
        {
            if (!IsRecurring) return null;
            return new TaskRecurrence
            {
                TaskId = taskId,
                RuleType = SelectedRuleType,
                IntervalN = (int)IntervalN,
                DaysMask = IsWeekly ? DaysMask : null,
                DayOfMonth = IsMonthly ? (int)DayOfMonth : null,
                EndDate = HasEndDate ? RecurrenceEndDate : (DateTime?)null,
                NextDue = null
            };
        }
    }
}
