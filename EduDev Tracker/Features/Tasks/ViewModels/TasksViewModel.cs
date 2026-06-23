using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Tasks.Views;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class TasksViewModel: BaseViewModel
    {

        private readonly INavigationService _navigation;
        private readonly IServiceProvider _services;
        private readonly ITaskService _taskService;
        private readonly RecurrenceProcessorService _recurrenceProcessor;
        private int _profileId;

        public TasksViewModel(ITaskService taskService,
            INavigationService navigation,
            IServiceProvider provider,
            RecurrenceProcessorService recurrenceProcessor)
        {
            _taskService = taskService;
            _services = provider;
            _navigation = navigation;
            _recurrenceProcessor = recurrenceProcessor;
            _profileId = SessionService.GetProfileId();
        }


        private List<TaskItem> _allTasks = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsListMode))]
        [NotifyPropertyChangedFor(nameof(IsCalendarMode))]
        [NotifyPropertyChangedFor(nameof(IsKanbanMode))]
        private string selectedViewMode = "Список";

        public bool IsListMode => SelectedViewMode == "Список";
        public bool IsCalendarMode => SelectedViewMode == "Календарь";
        public bool IsKanbanMode => SelectedViewMode == "Канбан";

        [RelayCommand]
        private void SelectViewMode(string mode) => SelectedViewMode = mode;

        [ObservableProperty]
        private string selectedPriorityFilter = "Все";

        [RelayCommand]
        private void SelectPriorityFilter(string filter)
        {
            SelectedPriorityFilter = filter;
            ApplyFilters();
        }

        [ObservableProperty] private string subtitle = string.Empty;
        [ObservableProperty] private string tasksCountText = string.Empty;

        private void UpdateSubtitle()
        {
            var today = _allTasks
                .Count(t => t.DueAt?.Date == DateTime.Today 
                && t.Status != Data.Models.TaskStatus.Done);

            var overdue = _allTasks
                .Count(t => t.DueAt?.Date < DateTime.Now 
                && t.Status != Data.Models.TaskStatus.Done);

            var active = _allTasks.Count(t => t.Status is Data.Models.TaskStatus.Open 
                or Data.Models.TaskStatus.InProgress);

            Subtitle = $"{today} задач на сегодня. {overdue} просрочена. {active} активных";
            TasksCountText = $"Всего: {_allTasks.Count}";
        }

        public ObservableCollection<TaskGroup> GroupedTasks { get; } = new();

        private List<TaskItem> _allFilteredListTasks = new();
        private int _listShownCount = ListPageSize;
        [ObservableProperty] private bool listHasMore;
        [ObservableProperty] private string listPageInfo = string.Empty;

        private void RebuildGroupedList(IEnumerable<TaskItem> tasks)
        {
            _allFilteredListTasks = tasks.ToList();
            RebuildGroupedListFromCache();
        }

        private void RebuildGroupedListFromCache()
        {
            GroupedTasks.Clear();
            var visible = _allFilteredListTasks.Take(_listShownCount).ToList();

            var overdue = visible
                .Where(t => t.DueAt < DateTime.Now && t.Status != TaskStatus.Done)
                .ToList();

            var today = visible
                .Where(t => t.DueAt?.Date == DateTime.Today && t.Status != TaskStatus.Done)
                .ToList();

            var tomorrow = visible
                .Where(t => t.DueAt?.Date == DateTime.Today.AddDays(1) && t.Status != TaskStatus.Done)
                .ToList();

            var later = visible
                .Where(t => t.DueAt?.Date > DateTime.Today.AddDays(1) && t.Status != TaskStatus.Done)
                .ToList();

            var noDate = visible
                .Where(t => t.DueAt is null && t.Status != TaskStatus.Done)
                .ToList();

            AddGroup("ПРОСРОЧЕНО", "#C0392B", overdue);
            AddGroup($"Сегодня — {DateTime.Today:d MMMM}", "#5B5FEF", today);
            AddGroup($"Завтра — {DateTime.Today.AddDays(1):d MMMM}", "#4A5A6A", tomorrow);
            AddGroup("Позже", "#4A5A6A", later);
            AddGroup("Без срока", "#4A5A6A", noDate);

            var total = _allFilteredListTasks.Count;
            var shown = Math.Min(_listShownCount, total);
            ListHasMore = total > _listShownCount;
            ListPageInfo = ListHasMore ? $"Показано {shown} из {total}" : string.Empty;
        }

        [RelayCommand]
        private void LoadMoreList()
        {
            _listShownCount += ListPageSize;
            RebuildGroupedListFromCache();
        }

        private void AddGroup(string title, string color, List<TaskItem> items)
        {
            System.Diagnostics.Debug.WriteLine($"[AddGroup] '{title}' items={items?.Count}");

            if (items == null || !items.Any()) return;

            var vmItems = items
                .Where(t => t != null)
                .Select(t =>
                {
                    System.Diagnostics.Debug.WriteLine($"[AddGroup] FromModel TaskId={t.Id}");
                    return TaskItemViewModel.FromModel(t);
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[AddGroup] Adding group with {vmItems.Count} vmItems");
            GroupedTasks.Add(new TaskGroup(title, color, vmItems));
            System.Diagnostics.Debug.WriteLine($"[AddGroup] '{title}' added OK");
        }

        private const int KanbanPageSize = 15;
        private const int KanbanActivePageSize = 10;
        private const int ListPageSize = 10;

        public ObservableCollection<TaskItemViewModel> OpenTasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> InProgressTasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> DoneTasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> CancelledTasks { get; } = new();

        private List<TaskItemViewModel> _allOpenVms = new();
        private List<TaskItemViewModel> _allInProgressVms = new();
        private List<TaskItemViewModel> _allDoneVms = new();
        private List<TaskItemViewModel> _allCancelledVms = new();
        private int _openOffset;
        private int _inProgressOffset;
        private int _doneOffset;
        private int _cancelledOffset;

        [ObservableProperty] private string openCount = "0";
        [ObservableProperty] private string inProgressCount = "0";
        [ObservableProperty] private string doneCount = "0";
        [ObservableProperty] private string cancelledCount = "0";
        [ObservableProperty] private bool openHasMore;
        [ObservableProperty] private string openPageInfo = string.Empty;
        [ObservableProperty] private bool inProgressHasMore;
        [ObservableProperty] private string inProgressPageInfo = string.Empty;
        [ObservableProperty] private bool doneHasMore;
        [ObservableProperty] private bool cancelledHasMore;
        [ObservableProperty] private string donePageInfo = string.Empty;
        [ObservableProperty] private string cancelledPageInfo = string.Empty;

        private void RebuildKanban(IEnumerable<TaskItem> tasks)
        {
            OpenTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();
            CancelledTasks.Clear();

            _allOpenVms.Clear();
            _allInProgressVms.Clear();
            _allDoneVms.Clear();
            _allCancelledVms.Clear();
            _openOffset = 0;
            _inProgressOffset = 0;
            _doneOffset = 0;
            _cancelledOffset = 0;

            foreach (var task in tasks)
            {
                var vm = TaskItemViewModel.FromModel(task);
                switch (task.Status)
                {
                    case TaskStatus.Open: _allOpenVms.Add(vm); break;
                    case TaskStatus.InProgress: _allInProgressVms.Add(vm); break;
                    case TaskStatus.Done: _allDoneVms.Add(vm); break;
                    case TaskStatus.Cancelled: _allCancelledVms.Add(vm); break;
                }
            }

            LoadOpenPage();
            LoadInProgressPage();
            LoadDonePage();
            LoadCancelledPage();

            OpenCount = _allOpenVms.Count.ToString();
            InProgressCount = _allInProgressVms.Count.ToString();
            DoneCount = _allDoneVms.Count.ToString();
            CancelledCount = _allCancelledVms.Count.ToString();
        }

        private void LoadOpenPage()
        {
            var batch = _allOpenVms.Skip(_openOffset).Take(KanbanActivePageSize).ToList();
            foreach (var vm in batch) OpenTasks.Add(vm);
            _openOffset += batch.Count;
            OpenHasMore = _openOffset < _allOpenVms.Count;
            OpenPageInfo = OpenHasMore
                ? $"Показано {_openOffset} из {_allOpenVms.Count}"
                : string.Empty;
        }

        private void LoadInProgressPage()
        {
            var batch = _allInProgressVms.Skip(_inProgressOffset).Take(KanbanActivePageSize).ToList();
            foreach (var vm in batch) InProgressTasks.Add(vm);
            _inProgressOffset += batch.Count;
            InProgressHasMore = _inProgressOffset < _allInProgressVms.Count;
            InProgressPageInfo = InProgressHasMore
                ? $"Показано {_inProgressOffset} из {_allInProgressVms.Count}"
                : string.Empty;
        }

        [RelayCommand] private void LoadMoreOpen() => LoadOpenPage();
        [RelayCommand] private void LoadMoreInProgress() => LoadInProgressPage();

        private void LoadDonePage()
        {
            var batch = _allDoneVms.Skip(_doneOffset).Take(KanbanPageSize).ToList();
            foreach (var vm in batch) DoneTasks.Add(vm);
            _doneOffset += batch.Count;
            DoneHasMore = _doneOffset < _allDoneVms.Count;
            DonePageInfo = DoneHasMore
                ? $"Показано {_doneOffset} из {_allDoneVms.Count}"
                : string.Empty;
        }

        private void LoadCancelledPage()
        {
            var batch = _allCancelledVms.Skip(_cancelledOffset).Take(KanbanPageSize).ToList();
            foreach (var vm in batch) CancelledTasks.Add(vm);
            _cancelledOffset += batch.Count;
            CancelledHasMore = _cancelledOffset < _allCancelledVms.Count;
            CancelledPageInfo = CancelledHasMore
                ? $"Показано {_cancelledOffset} из {_allCancelledVms.Count}"
                : string.Empty;
        }

        [RelayCommand]
        private void LoadMoreDone() => LoadDonePage();

        [RelayCommand]
        private void LoadMoreCancelled() => LoadCancelledPage();

        [ObservableProperty]
        private DateTime? selectedCalendarDate;

        [ObservableProperty]
        private ObservableCollection<TaskItemViewModel> tasksForSelectedDay = new();

        [ObservableProperty]
        private string selectedDayTitle = string.Empty;

        [RelayCommand]
        private void SelectCalendarDay(DateTime date)
        {
            foreach (var day in CalendarDays)
                day.IsSelected = false;

            var selected = CalendarDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (selected != null) selected.IsSelected = true;

            SelectedCalendarDate = date;
            SelectedDayTitle = date.ToString("d MMMM yyyy", new CultureInfo("ru-RU"));

            var filtered = _allTasks
                .Where(t => t.DueAt.HasValue && t.DueAt.Value.Date == date.Date)
                .Select(TaskItemViewModel.FromModel)
                .ToList();

            TasksForSelectedDay = new ObservableCollection<TaskItemViewModel>(filtered);
        }

        [ObservableProperty]
        private string currentMonthTitle = string.Empty;
        private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        public ObservableCollection<CalendarDayViewModel> CalendarDays { get; } = new();

        [RelayCommand]
        private void PrevMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RefreshCalendar();
        }

        [RelayCommand]
        private void NextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RefreshCalendar();
        }

        private void RefreshCalendar()
        {
            CurrentMonthTitle = _currentMonth.ToString("MMMM yyyy");
            CalendarDays.Clear();

            int offset = ((int)_currentMonth.DayOfWeek + 6) % 7;

            for(int i = 0; i < offset; i++)
            {
                var d = _currentMonth.AddDays(-(offset - i));
                CalendarDays.Add(MakeDayVm(d, isOther: true));
            }

            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            for(int i = 0; i < daysInMonth; i++)
            {
                var d = _currentMonth.AddDays(i);
                var vm = MakeDayVm(d);
                vm.IsToday = d.Date == DateTime.Today;

                var dayTasks = _allTasks.Where(t => t.DueAt?.Date == d.Date).ToList();
                vm.HasAny = dayTasks.Any();
                vm.HasCritical = dayTasks.Any(t => t.Priority == TaskPriority.Urgent);
                vm.HasHigh = dayTasks.Any(t => t.Priority == TaskPriority.High);

                CalendarDays.Add(vm);
            }

            while (CalendarDays.Count % 7 != 0)
            {
                var d = _currentMonth.AddDays(daysInMonth + CalendarDays.Count % 7);
                CalendarDays.Add(MakeDayVm(d, isOther: true));
            }
        }
        private static CalendarDayViewModel MakeDayVm(DateTime d, bool isOther = false) => new()
        {
            Date = d,
            DayNumber = d.Day.ToString(),
            IsOtherMonth = isOther
        };

        private void ApplyFilters()
        {
            IEnumerable<TaskItem> filtered = _allTasks;

            filtered = SelectedPriorityFilter switch
            {
                "Критичный" => filtered
                .Where(t => t.Priority == TaskPriority.Urgent),
                "Высокий" => filtered
                .Where(t => t.Priority == TaskPriority.High),
                "Средний" => filtered
                .Where(t => t.Priority == TaskPriority.Medium),
                "Низкий" => filtered
                .Where(t => t.Priority == TaskPriority.Low),
                "Просроченные" => filtered
                .Where(t => t.DueAt < DateTime.Now && t.Status != TaskStatus.Done),
                _ => filtered
            };

            var list = filtered
                .OrderBy(t => t.DueAt)
                .ThenBy(t => t.Priority)
                .ToList();

            _listShownCount = ListPageSize;
            RebuildGroupedList(list);
            RebuildKanban(list);
            RefreshCalendar();
            UpdateSubtitle();
        }

        [RelayCommand]
        private async Task ToggleTask(int taskId)
        {
            var task = _allTasks.FirstOrDefault(t => t.Id == taskId);
            if (task is null) return;

            task.Status = task.Status == TaskStatus.Done ? TaskStatus.Open : TaskStatus.Done;
            task.CompletedAt = task.Status == TaskStatus.Done ? DateTime.UtcNow : null;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskService.SaveAsync(task);
            ApplyFilters();
        }

        [RelayCommand]
        private async Task OpenTaskDetails(int id)
        {
            //var page = _services.GetRequiredService<TaskDetailsPage>();

            //if (page.BindingContext is TaskDetailsViewModel vm)
                //await vm.InitializeAsync(new Dictionary<string, object> { { "taskId", id } });

            //await _navigation.PushModalAsync(page);

            await _navigation.GoToAsync($"{nameof(TaskDetailsPage)}?taskId={id}");
        }

        [RelayCommand]
        private async Task EditTask(int taskId)
        {
            //var page = _services.GetRequiredService<TaskDetailsPage>();

            //if (page.BindingContext is TaskDetailsViewModel vm)
                //await vm.InitializeAsync(new Dictionary<string, object> { { "taskId", taskId } });

            //await _navigation.PushModalAsync(page);

            await _navigation.GoToAsync($"{nameof(TaskDetailsPage)}?taskId={taskId}");
        }

        [RelayCommand]
        private async Task DeleteTask(int taskId)
        {
            bool confirmation = await Application.Current.MainPage.DisplayAlertAsync("Подтвердите свои действия",
                "Вы уверены в удалении задачи?", "Да", "Нет");
            if (confirmation)
            {
                await _taskService.DeleteAsync(taskId);
                _allTasks.RemoveAll(t => t.Id == taskId);
                ApplyFilters();
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task OpenReminder(int taskId)
            => await _navigation.GoToAsync($"{nameof(ReminderSettingsPage)}?taskId={taskId}");

        [RelayCommand]
        private async Task OpenAddTask()
        {
            //var modal = _services.GetRequiredService<AddTaskPage>();
            //await _navigation.PushModalAsync(modal);

            await _navigation.GoToAsync(nameof(AddTaskPage));
        }

        [RelayCommand]
        private async Task Load()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                try
                {
                    await _recurrenceProcessor.ProcessAsync(_profileId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessAsync] ERROR: {ex}");
                }

                _allTasks = await _taskService.GetActiveAsync(_profileId);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TasksViewModel.Load] CRASH: {ex}");
                try
                {
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlertAsync("Ошибка загрузки", ex.Message, "OK");
                }
                catch (Exception alertEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Load] Alert failed: {alertEx.Message}");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        public override async Task InitializeAsync(IDictionary<string, object>? query = null)
        {
            await LoadCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task MoveTaskForward(int taskId)
        {
            var task = await _taskService.GetByIdWithChildrenAsync(taskId);
            task.Status = task.Status switch
            {
                TaskStatus.Open => TaskStatus.InProgress,
                TaskStatus.InProgress => TaskStatus.Done,
                _ => task.Status
            };
            await _taskService.SaveAsync(task);
            await Load();
        }

        [RelayCommand]
        private async Task MoveTaskBack(int taskId)
        {
            var task = await _taskService.GetByIdWithChildrenAsync(taskId);
            task.Status = task.Status switch
            {
                TaskStatus.Done => TaskStatus.InProgress,
                TaskStatus.InProgress => TaskStatus.Open,
                _ => task.Status
            };
            await _taskService.SaveAsync(task);
            await Load();
        }

    }
}
