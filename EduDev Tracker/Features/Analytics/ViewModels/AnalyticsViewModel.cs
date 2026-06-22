using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Features.Analytics.Models;
using EduDev_Tracker.Services.Analytics;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Analytics.ViewModels
{
    public record ModuleFilter(string Label, ReportModule Module);
    public partial class AnalyticsViewModel: BaseViewModel
    {
        private readonly IAnalyticsService _service;
        private int profileId;

        [ObservableProperty] private AnalyticsPeriod selectedPeriod = AnalyticsPeriod.Week;
        [ObservableProperty] private DateTime customFrom = DateTime.Today.AddDays(-6);
        [ObservableProperty] private DateTime customTo = DateTime.Today;
        [ObservableProperty] private bool showCustomRange = false;

        [ObservableProperty] private string habitsText = "—";
        [ObservableProperty] private string habitsDelta = "";
        [ObservableProperty] private string tasksText = "—";
        [ObservableProperty] private string tasksDelta = "";
        [ObservableProperty] private string pomodoroText = "—";
        [ObservableProperty] private string pomodoroDelta = "";
        [ObservableProperty] private string notesText = "—";
        [ObservableProperty] private string notesDelta = "";
        [ObservableProperty] private string pomodoroSubText = "";

        [ObservableProperty] private List<DayProductivityPoint> chartPoints = [];

        [ObservableProperty] private List<ModuleRow> moduleTable = [];

        [ObservableProperty] private List<string> insights = [];
        [ObservableProperty] private List<string> recommendations = [];

        [ObservableProperty] private ReportFormat selectedFormat = ReportFormat.Pdf;
        [ObservableProperty] private bool isPdfSelected = true;
        [ObservableProperty] private bool isExcelSelected = false;
        [ObservableProperty] private bool isGenerating = false;
        [ObservableProperty] private string exportStatus = "";
        [ObservableProperty] private bool hasExportStatus = false;

        [ObservableProperty] private bool isLoading = false;
        [ObservableProperty] private string periodLabel = "";

        [ObservableProperty] private ReportModule selectedModule = ReportModule.All;

        public List<ModuleFilter> ModuleFilters { get; } =
        [
            new("Все модули",  ReportModule.All),
            new("Привычки",    ReportModule.Habits),
            new("Задачи",      ReportModule.Tasks),
            new("Помодоро",    ReportModule.Pomodoro),
            new("Заметки",     ReportModule.Notes),
        ];

        [RelayCommand]
        private async Task SelectModuleAsync(string moduleStr)
        {
            SelectedModule = moduleStr switch
            {
                "Habits" => ReportModule.Habits,
                "Tasks" => ReportModule.Tasks,
                "Pomodoro" => ReportModule.Pomodoro,
                "Notes" => ReportModule.Notes,
                _ => ReportModule.All
            };
            await LoadAsync();
        }

        private AnalyticsReport? _lastReport;

        public AnalyticsViewModel(IAnalyticsService service)
        {
            _service = service;
            profileId = SessionService.GetProfileId();
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task SelectPeriodAsync(string period)
        {
            SelectedPeriod = period switch
            {
                "week" => AnalyticsPeriod.Week,
                "month" => AnalyticsPeriod.Month,
                "custom" => AnalyticsPeriod.Custom,
                _ => AnalyticsPeriod.Week
            };
            ShowCustomRange = SelectedPeriod == AnalyticsPeriod.Custom;

            if (SelectedPeriod != AnalyticsPeriod.Custom)
                await LoadAsync();
        }

        [RelayCommand]
        private async Task ApplyCustomRangeAsync()
        {
            if (CustomFrom > CustomTo)
            {
                ExportStatus = "Дата начала не может быть позже даты окончания";
                HasExportStatus = true;
                return;
            }
            await LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            HasExportStatus = false;

            try
            {
                var (from, to) = GetDateRange();
                var report = await _service.BuildReportAsync(profileId, from, to, SelectedModule);
                _lastReport = report;
                ApplyReport(report);
            }
            catch (Exception ex)
            {
                ExportStatus = $"Ошибка загрузки: {ex.Message}";
                HasExportStatus = true;
                System.Diagnostics.Debug.WriteLine($"[Analytics] {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private (DateTime from, DateTime to) GetDateRange() => SelectedPeriod switch
        {
            AnalyticsPeriod.Week => (DateTime.Today.AddDays(-6), DateTime.Today),
            AnalyticsPeriod.Month => (DateTime.Today.AddDays(-29), DateTime.Today),
            AnalyticsPeriod.Custom => (CustomFrom.Date, CustomTo.Date),
            _ => (DateTime.Today.AddDays(-6), DateTime.Today)
        };

        private void ApplyReport(AnalyticsReport r)
        {
            PeriodLabel = r.PeriodLabel;

            // Метрики
            HabitsText = $"{r.HabitsCompleted}";
            HabitsDelta = FormatDelta(r.HabitsCompleted, r.HabitsCompletedPrev, "к прошлой неделе");

            TasksText = r.TasksClosed.ToString();
            TasksDelta = r.TasksOverdue > 0
                ? $"{r.TasksOverdue} просрочено"
                : "все в срок";

            PomodoroText = r.PomodoroSessions.ToString();
            PomodoroSubText = $"чистое время {r.PomodoroMinutes / 60} ч {r.PomodoroMinutes % 60} мин";
            PomodoroDelta = FormatDelta(r.PomodoroSessions, r.PomodoroSessionsPrev, "к прошлому периоду");

            NotesText = r.NotesCreated.ToString();
            NotesDelta = $"{r.NotesPinned} закреплено";

            // График
            ChartPoints = r.DailyChart;

            // Таблица
            ModuleTable = r.ModuleTable;

            // Инсайты
            Insights = r.Insights;
            Recommendations = r.Recommendations;
        }

        private static string FormatDelta(int cur, int prev, string suffix)
        {
            if (prev == 0) return "";
            var diff = cur - prev;
            return diff >= 0 ? $"+{diff} {suffix}" : $"{diff} {suffix}";
        }

        [RelayCommand]
        private void SelectFormat(string fmt)
        {
            SelectedFormat = fmt == "pdf" ? ReportFormat.Pdf : ReportFormat.Csv;
            IsPdfSelected = SelectedFormat == ReportFormat.Pdf;
            IsExcelSelected = SelectedFormat == ReportFormat.Csv;
        }

        [RelayCommand]
        private async Task GenerateReportAsync()
        {
            if (_lastReport is null) return;

            IsGenerating = true;
            HasExportStatus = false;

            try
            {
                string path = SelectedFormat == ReportFormat.Pdf
                    ? await _service.ExportPdfAsync(_lastReport)
                    : await _service.ExportExcelAsync(_lastReport);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Экспорт отчёта EduDev Tracker",
                    File = new ShareFile(path)
                });

                ExportStatus = "Отчёт готов ✓";
                HasExportStatus = true;
            }
            catch (Exception ex)
            {
                ExportStatus = $"Ошибка: {ex.Message}";
                HasExportStatus = true;
            }
            finally
            {
                IsGenerating = false;
            }
        }
    }
}
