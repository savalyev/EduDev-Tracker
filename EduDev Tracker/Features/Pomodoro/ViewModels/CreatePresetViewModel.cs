using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Pomodoro;
using System.Collections.ObjectModel;

namespace EduDev_Tracker.Features.Pomodoro.ViewModels
{
    public class CyclePreviewItem
    {
        public string Label { get; set; } = "";
        public Color BadgeColor { get; set; } = Color.FromArgb("#1E293B");
        public Color TextColor { get; set; } = Color.FromArgb("#F1F5F9");
    }

    public partial class CreatePresetViewModel: ObservableObject
    {
        private readonly IPomodoroService _pomodoroService;

        public event Action<PomodoroPreset>? PresetCreated;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNameError))]
        private string name = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNameError))]
        private string nameError = "";

        public bool HasNameError => !string.IsNullOrEmpty(NameError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalCycleTime))]
        [NotifyPropertyChangedFor(nameof(TotalWorkTime))]
        [NotifyPropertyChangedFor(nameof(TotalBreakTime))]
        [NotifyPropertyChangedFor(nameof(CyclePreviewItems))]
        private int workMinutes = 25;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalCycleTime))]
        [NotifyPropertyChangedFor(nameof(TotalBreakTime))]
        [NotifyPropertyChangedFor(nameof(CyclePreviewItems))]
        private int shortBreakMin = 5;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalCycleTime))]
        [NotifyPropertyChangedFor(nameof(TotalBreakTime))]
        [NotifyPropertyChangedFor(nameof(CyclePreviewItems))]
        private int longBreakMin = 15;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CyclesDescription))]
        [NotifyPropertyChangedFor(nameof(TotalCycleTime))]
        [NotifyPropertyChangedFor(nameof(TotalWorkTime))]
        [NotifyPropertyChangedFor(nameof(TotalBreakTime))]
        [NotifyPropertyChangedFor(nameof(CyclePreviewItems))]
        private int cyclesToLong = 4;

        [ObservableProperty] private bool isDefault;

        public string CyclesDescription =>
       $"Длинный перерыв каждые {CyclesToLong} {PluralCycles(CyclesToLong)}";

        public string TotalCycleTime
        {
            get
            {
                int total = WorkMinutes * CyclesToLong
                          + ShortBreakMin * (CyclesToLong - 1)
                          + LongBreakMin;
                return FormatMinutes(total);
            }
        }

        public string TotalWorkTime => FormatMinutes(WorkMinutes * CyclesToLong);

        public string TotalBreakTime
        {
            get
            {
                int breaks = ShortBreakMin * (CyclesToLong - 1) + LongBreakMin;
                return FormatMinutes(breaks);
            }
        }

        public ObservableCollection<CyclePreviewItem> CyclePreviewItems
        {
            get
            {
                var items = new ObservableCollection<CyclePreviewItem>();
                for (int i = 0; i < CyclesToLong; i++)
                {
                    items.Add(new CyclePreviewItem
                    {
                        Label = $"🟢 {WorkMinutes}м",
                        BadgeColor = Color.FromArgb("#0D2A2E"),
                        TextColor = Color.FromArgb("#2DD4BF")
                    });

                    if (i < CyclesToLong - 1)
                    {
                        items.Add(new CyclePreviewItem
                        {
                            Label = $"🔵 {ShortBreakMin}м",
                            BadgeColor = Color.FromArgb("#0D1E3A"),
                            TextColor = Color.FromArgb("#60A5FA")
                        });
                    }
                }
                items.Add(new CyclePreviewItem
                {
                    Label = $"🟣 {LongBreakMin}м",
                    BadgeColor = Color.FromArgb("#1A0D3A"),
                    TextColor = Color.FromArgb("#A78BFA")
                });
                return items;
            }
        }

        public CreatePresetViewModel(IPomodoroService pomodoroService)
        {
            _pomodoroService = pomodoroService;
        }

        [RelayCommand]
        private void IncreaseWork()
        {
            if (WorkMinutes < 90) WorkMinutes += 5;
        }

        [RelayCommand]
        private void DecreaseWork()
        {
            if (WorkMinutes > 5) WorkMinutes -= 5;
        }

        [RelayCommand]
        private void IncreaseShortBreak()
        {
            if (ShortBreakMin < 30) ShortBreakMin++;
        }

        [RelayCommand]
        private void DecreaseShortBreak()
        {
            if (ShortBreakMin > 1) ShortBreakMin--;
        }

        [RelayCommand]
        private void IncreaseLongBreak()
        {
            if (LongBreakMin < 60) LongBreakMin += 5;
        }

        [RelayCommand]
        private void DecreaseLongBreak()
        {
            if (LongBreakMin > 5) LongBreakMin -= 5;
        }

        [RelayCommand]
        private void IncreaseCycles()
        {
            if (CyclesToLong < 8) CyclesToLong++;
        }

        [RelayCommand]
        private void DecreaseCycles()
        {
            if (CyclesToLong > 1) CyclesToLong--;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Введите название пресета";
                return;
            }
            if (Name.Trim().Length < 2)
            {
                NameError = "Название слишком короткое";
                return;
            }
            NameError = "";

            int profileId = Preferences.Default.Get("active_profile_id", 0);

            if (IsDefault)
            {
                var existing = await _pomodoroService.GetPresetsAsync(profileId);
                foreach (var p in existing)
                {
                    if (p.IsDefault)
                    {
                        p.IsDefault = false;
                        await _pomodoroService.SavePresetAsync(p);
                    }
                }
            }

            var preset = new PomodoroPreset
            {
                ProfileId = profileId,
                Name = Name.Trim(),
                WorkMinutes = WorkMinutes,
                ShortBreakMin = ShortBreakMin,
                LongBreakMin = LongBreakMin,
                CyclesToLong = CyclesToLong,
                IsDefault = IsDefault
            };

            await _pomodoroService.SavePresetAsync(preset);
            PresetCreated?.Invoke(preset);

            await Shell.Current.Navigation.PopModalAsync(animated: true);
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.Navigation.PopModalAsync(animated: true);
        }
        private static string FormatMinutes(int totalMinutes)
        {
            if (totalMinutes < 60)
                return $"{totalMinutes}м";
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            return m == 0 ? $"{h}ч" : $"{h}ч {m}м";
        }

        private static string PluralCycles(int n) => n switch
        {
            1 => "цикл",
            2 or 3 or 4 => "цикла",
            _ => "циклов"
        };
    }
}
