using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notification;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitDetailsViewModel: BaseViewModel
    {
        private readonly IHabitService _habitService;
        private readonly INotificationService _habitNotificationService;
        private Habit _habit;
        private readonly INavigationService _navigation;

        [ObservableProperty] private string titleHabit;
        [ObservableProperty] private string description;
        [ObservableProperty] private string schedule;
        [ObservableProperty] private string timeOfDay;
        [ObservableProperty] private string type;
        [ObservableProperty] private string streakText;
        [ObservableProperty] private string icon;
        [ObservableProperty] private bool isFrozen;
        [ObservableProperty] private bool isArchived;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsViewing))]
        private bool isEditing;

        public bool IsViewing => !isEditing;

        public string FreezeButtonText => IsFrozen ? "❄️ Разморозить" : "🧊 Заморозить";

        partial void OnIsFrozenChanged(bool value)
            => OnPropertyChanged(nameof(FreezeButtonText));

        [ObservableProperty] private string editTitle;
        [ObservableProperty] private string editDescription;

        public HabitDetailsViewModel(IHabitService habitService,
            INotificationService habitNotificationService,
            INavigationService navigation)
        {
            _habitService = habitService;
            _habitNotificationService = habitNotificationService;
            _navigation = navigation;
        }

        public async Task InitializeAsync(int habitId)
        {
            _habit = await _habitService.GetByIdWithChildrenAsync(habitId);
            if (_habit == null) return;

            TitleHabit = _habit.Title;
            Description = _habit.Description ?? "—";
            Type = _habit.Type.ToString();
            IsFrozen = _habit.IsFrozen;
            IsArchived = _habit.IsArchived;

            var schedule = await _habitService.GetScheduleAsync(habitId);
            Schedule = schedule?.GetFormattedDays() ?? "—";
            TimeOfDay = schedule?.TimeOfDay ?? "—";

            var streak = await _habitService.GetCurrentStreakAsync(habitId);
            StreakText = streak > 0 ? $"🔥 {streak}" : "—";

            Icon = _habit.Icon ?? "habit_icon.png";

            EditTitle = _habit.Title;
            EditDescription = _habit.Description ?? "";
        }

        [RelayCommand]
        private void StartEditing() => IsEditing = true;

        [RelayCommand]
        private void CancelEditing()
        {
            EditTitle = TitleHabit;
            EditDescription = Description;
            IsEditing = false;
        }

        [RelayCommand]
        private async Task SaveEditAsync()
        {
            if (string.IsNullOrWhiteSpace(EditTitle)) return;

            _habit.Title = EditTitle;
            _habit.Description = EditDescription;
            await _habitService.UpdateAsync(_habit);

            TitleHabit = EditTitle;
            Description = EditDescription;
            IsEditing = false;
        }

        [RelayCommand]
        private async Task DeleteHabitAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Удалить привычку",
                $"Удалить «{TitleHabit}»? Все логи будут удалены безвозвратно.",
                "Удалить", "Отмена");

            if (!confirm) return;


            await _habitService.DeleteAsync(_habit.Id);
            await _habitNotificationService.CancelHabitNotificationAsync(_habit.Id);
            await CloseAsync();
        }

        [RelayCommand]
        private async Task FreezeHabitAsync()
        {
            if (IsFrozen)
            {
                bool confirm = await Shell.Current.DisplayAlertAsync(
                    "Разморозить привычку",
                    "Привычка снова станет активной.",
                    "Разморозить", "Отмена");
                if (!confirm) return;

                _habit.IsFrozen = false;
                _habit.FrozenUntil = DateTime.MinValue;
            }
            else
            {
                string input = await Shell.Current.DisplayPromptAsync(
                    "Заморозить привычку",
                    "На сколько дней заморозить? (оставьте пустым — бессрочно)",
                    accept: "Заморозить",
                    cancel: "Отмена",
                    placeholder: "Например: 7",
                    keyboard: Keyboard.Numeric);

                if (input == null) return;

                _habit.IsFrozen = true;
                _habit.FrozenUntil = int.TryParse(input, out int days) && days > 0
                    ? DateTime.Today.AddDays(days)
                    : DateTime.MaxValue;
            }

            await _habitService.UpdateAsync(_habit);
            IsFrozen = _habit.IsFrozen;
        }

        [RelayCommand]
        private async Task ArchiveHabitAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Архивировать привычку",
                "Привычка переместится в архив. Все логи и серия сохранятся.",
                "Архивировать", "Отмена");

            if (!confirm) return;

            _habit.IsArchived = true;
            await _habitService.UpdateAsync(_habit);
            await _habitNotificationService.CancelHabitNotificationAsync(_habit.Id);
            await CloseAsync();
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            await _navigation.PopModalAsync();
        }

    }
}
