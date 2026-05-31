using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Habits;
using System.Collections.ObjectModel;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class ArchivedHabitsViewModel : BaseViewModel
    {
        private readonly IHabitService _habitService;

        public ObservableCollection<ArchivedHabitItem> ArchivedHabits { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
        private bool isEmpty;

        public bool IsNotEmpty => !IsEmpty;

        public ArchivedHabitsViewModel(IHabitService habitService)
        {
            _habitService = habitService;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                ArchivedHabits.Clear();

                System.Diagnostics.Debug.WriteLine("[Archive] LoadAsync вызван");

                var habits = await _habitService.GetArchivedAsync(1);

                System.Diagnostics.Debug.WriteLine($"[Archive] Найдено: {habits.Count}");

                foreach (var habit in habits)
                    ArchivedHabits.Add(new ArchivedHabitItem(habit));

                IsEmpty = ArchivedHabits.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Archive] Ошибка: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RestoreAsync(int habitId)
        {
            await _habitService.ArchiveAsync(habitId, archived: false);
            await LoadAsync();
        }

        [RelayCommand]
        private async Task DeleteForeverAsync(int habitId)
        {
            var item = ArchivedHabits.FirstOrDefault(h => h.HabitId == habitId);
            if (item == null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Удалить навсегда",
                $"Удалить «{item.Title}»? Все логи и серия будут удалены безвозвратно.",
                "Удалить", "Отмена");

            if (!confirm) return;

            await _habitService.DeleteAsync(habitId);
            await LoadAsync();
        }

        [RelayCommand]
        private async Task CloseAsync()
            => await Shell.Current.CurrentPage.Navigation.PopModalAsync();
    }

    public class ArchivedHabitItem
    {
        public int HabitId { get; }
        public string Title { get; }
        public string Description { get; }
        public string Icon { get; }
        public string Type { get; }
        public string ArchivedDate { get; }

        public ArchivedHabitItem(Habit habit)
        {
            HabitId = habit.Id;
            Title = habit.Title;
            Description = habit.Description ?? "—";
            Icon = habit.Icon ?? "habit_icon.png";
            Type = habit.Type.ToString();
            ArchivedDate = habit.UpdatedAt.ToString("dd.MM.yyyy");
        }
    }
}