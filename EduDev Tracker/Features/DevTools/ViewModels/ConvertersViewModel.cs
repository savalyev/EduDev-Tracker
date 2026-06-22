using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EduDev_Tracker.Features.DevTools.ViewModels
{
    public partial class HistoryItemVm : ObservableObject
    {
        public int Id { get; set; }
        public string TypeIcon { get; set; } = "";
        public string TypeLabel { get; set; } = "";
        public string Input { get; set; } = "";
        public string Output { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public ConversionType Type { get; set; }
    }
    public partial class ConvertersViewModel: BaseViewModel
    {
        private readonly IConversionService _service;
        private int _profileId;
        private System.Timers.Timer? _debounce;

        [ObservableProperty] private int selectedTabIndex; //вкладочки

        [ObservableProperty] private ObservableCollection<HistoryItemVm> history = new();
        [ObservableProperty] private bool hasHistory;
        [ObservableProperty] private string historyTitle = "История за сегодня";

        //числа
        [ObservableProperty] private string numeralInput = "";
        [ObservableProperty] private int customBase = 16;
        [ObservableProperty] private string numeralBin = "—";
        [ObservableProperty] private string numeralOct = "—";
        [ObservableProperty] private string numeralDec = "—";
        [ObservableProperty] private string numeralHex = "—";
        [ObservableProperty] private string numeralCustom = "";
        [ObservableProperty] private bool showNumeralCustom;
        [ObservableProperty] private string numeralError = "";
        [ObservableProperty] private bool hasNumeralError;

        //Цвет
        [ObservableProperty] private string colorInput = "";
        [ObservableProperty] private string colorHex = "—";
        [ObservableProperty] private string colorRgb = "—";
        [ObservableProperty] private string colorHsl = "—";
        [ObservableProperty] private Color colorPreview = Color.FromArgb("#1E293B");
        [ObservableProperty] private string colorError = "";
        [ObservableProperty] private bool hasColorError;

        //Время
        [ObservableProperty] private string timeInput = "";
        [ObservableProperty] private string fromZoneId = "UTC";
        [ObservableProperty] private string toZoneId = "Europe/Moscow";
        [ObservableProperty] private string timeResult = "—";
        [ObservableProperty] private string timeError = "";
        [ObservableProperty] private bool hasTimeError;
        [ObservableProperty] private ObservableCollection<string> availableZones = new();

        //JSON / XML
        [ObservableProperty] private string jsonXmlInput = "";
        [ObservableProperty] private string jsonXmlOutput = "";
        [ObservableProperty] private string jsonXmlError = "";
        [ObservableProperty] private bool hasJsonXmlError;
        [ObservableProperty] private bool isJsonMode = true;  // true=JSON, false=XML

        //URL
        [ObservableProperty] private string urlInput = "";
        [ObservableProperty] private string urlOutput = "";
        [ObservableProperty] private string urlError = "";
        [ObservableProperty] private bool hasUrlError;

        public string[] TabTips { get; } =
    {
        // Числа
        "Вводи число в десятичной (255), двоичной (0b11111111), восьмеричной (0o377) или шестнадцатеричной (0xFF) системе. Результат пересчитывается автоматически.\n\nПримеры:\n• 255 → FF (hex), 11111111 (bin)\n• 0xFF → 255 (dec)\n• 0b1010 → 10 (dec)",

        // Цвет
        "Вводи цвет в любом формате: HEX (#FF5733), RGB (rgb(255, 87, 51)) или HSL (hsl(11, 100%, 60%)).\n\nПримеры:\n• #4F46E5 → rgb(79, 70, 229)\n• rgb(0, 128, 255) → #0080FF\n• hsl(120, 50%, 50%) → #40BF40",

        // Время
        "Конвертируй время между часовыми поясами. Введи дату и время в поле, выбери исходный и целевой пояс.\n\nПримеры:\n• 2024-01-15 10:00 (UTC → MSK) → 13:00 (UTC+3)\n• 15:00 MSK → 12:00 UTC",

        // JSON/XML
        "Форматируй и конвертируй JSON ↔ XML. Переключатель сверху меняет режим.\n\nПримеры:\n• JSON: {\"name\":\"John\",\"age\":30}\n• XML: <root><name>John</name></root>\n• Форматирование «склеенного» JSON и XML",

        // URL
        "URL-encode кодирует специальные символы для безопасной передачи в URL. Decode — обратная операция.\n\nПримеры:\n• «Привет мир» → %D0%9F%D1%80%D0%B8%D0%B2%D0%B5%D1%82%20%D0%BC%D0%B8%D1%80\n• API ключи с & и = в query-строке"
    };

        public string CurrentTip => TabTips[SelectedTabIndex];

        public ConvertersViewModel(IConversionService service)
        {
            _service = service;
            _profileId = SessionService.GetProfileId();
        }

        public async Task InitializeAsync()
        {
            LoadTimezones();
            TimeInput = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            await LoadHistoryAsync();
        }

        private void LoadTimezones()
        {
            var zones = TimeZoneInfo.GetSystemTimeZones()
                .Select(z => z.Id)
                .ToList();
            AvailableZones = new ObservableCollection<string>(zones);
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentTip));
        }

        [RelayCommand]
        private void SelectTab(string indexStr)
        {
            if (int.TryParse(indexStr, out var idx))
                SelectedTabIndex = idx;
        }

        partial void OnNumeralInputChanged(string value) => ConvertNumeralDebounced();
        partial void OnCustomBaseChanged(int value)
        {
            OnPropertyChanged(nameof(CustomBaseLabel));
            ConvertNumeralDebounced();
        }

        public List<int> AvailableBases { get; } =
            Enumerable.Range(2, 35).ToList(); 


        public string CustomBaseLabel => CustomBase switch
        {
            2 => "2 (двоичная)",
            8 => "8 (восьмеричная)",
            10 => "10 (десятичная)",
            16 => "16 (шестнадцатеричная)",
            _ => $"{CustomBase} (основание)"
        };

        private void ConvertNumeralDebounced()
        {
            _debounce?.Stop();
            _debounce?.Dispose();
            _debounce = new System.Timers.Timer(300) { AutoReset = false };
            _debounce.Elapsed += async (_, _) =>
            {
                _debounce?.Dispose();
                await MainThread.InvokeOnMainThreadAsync(DoConvertNumeral);
            };
            _debounce.Start();
        }

        private async Task DoConvertNumeral()
        {
            if (string.IsNullOrWhiteSpace(NumeralInput))
            {
                NumeralBin = NumeralOct = NumeralDec = NumeralHex = "—";
                NumeralCustom = ""; HasNumeralError = false; NumeralError = "";
                return;
            }

            var r = _service.ConvertNumeral(NumeralInput, CustomBase);

            if (!r.Success)
            {
                HasNumeralError = true;
                NumeralError = r.Error ?? "";
                NumeralBin = NumeralOct = NumeralDec = NumeralHex = "—";
                NumeralCustom = "";
                return;
            }

            HasNumeralError = false; NumeralError = "";
            NumeralBin = r.Binary;
            NumeralOct = r.Octal;
            NumeralDec = r.Decimal;
            NumeralHex = r.Hex;

            bool isStandard = CustomBase is 2 or 8 or 10 or 16;
            ShowNumeralCustom = !isStandard;
            NumeralCustom = isStandard ? "" : (r.Custom ?? "—");

            var summary = $"{NumeralInput.Trim()} → {r.Hex} (hex)";
            await SaveAsync(ConversionType.Numeral, NumeralInput.Trim(), summary,
                $"{{\"toBase\":{CustomBase}}}");
        }

        partial void OnColorInputChanged(string value) => DoConvertColorDebounced();

        private void DoConvertColorDebounced()
        {
            _debounce?.Stop();
            _debounce?.Dispose();
            _debounce = new System.Timers.Timer(300) { AutoReset = false };
            _debounce.Elapsed += async (_, _) =>
                await MainThread.InvokeOnMainThreadAsync(DoConvertColor);
            _debounce.Start();
        }

        private async Task DoConvertColor()
        {
            if (string.IsNullOrWhiteSpace(ColorInput))
            {
                ColorHex = ColorRgb = ColorHsl = "—";
                ColorPreview = Color.FromArgb("#1E293B");
                HasColorError = false; ColorError = "";
                return;
            }

            // Частичный HEX — не ошибка, просто ещё вводят (допустимые длины: 3 или 6 без #)
            var trimmed = ColorInput.Trim();
            if (trimmed.StartsWith('#'))
            {
                var hexLen = trimmed.TrimStart('#').Length;
                if (hexLen is not (3 or 6))
                {
                    ColorHex = ColorRgb = ColorHsl = "—";
                    ColorPreview = Color.FromArgb("#1E293B");
                    HasColorError = false; ColorError = "";
                    return;
                }
            }

            var r = _service.ConvertColor(ColorInput);

            if (!r.Success)
            {
                HasColorError = true; ColorError = r.Error ?? "";
                ColorPreview = Color.FromArgb("#1E293B");
                return;
            }

            HasColorError = false; ColorError = "";
            ColorHex = r.Hex;
            ColorRgb = r.Rgb;
            ColorHsl = r.Hsl;
            ColorPreview = Color.FromRgb(r.R, r.G, r.B);

            await SaveAsync(ConversionType.Color, ColorInput, $"{ColorInput} → {r.Hex}");
        }

        [RelayCommand]
        private async Task ConvertTimeAsync()
        {
            var r = _service.ConvertTime(TimeInput, FromZoneId, ToZoneId);
            if (!r.Success) { HasTimeError = true; TimeError = r.Error ?? ""; return; }

            HasTimeError = false; TimeError = "";
            TimeResult = r.Output;
            await SaveAsync(ConversionType.Time, TimeInput, r.Output,
                $"{{\"from\":\"{FromZoneId}\",\"to\":\"{ToZoneId}\"}}");
        }

        [RelayCommand]
        private void UseNow() => TimeInput = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        partial void OnJsonXmlInputChanged(string value) => DoJsonXmlDebounced();

        private bool _suppressModeDebounce = false;
        partial void OnIsJsonModeChanged(bool value)
        {
            if (_suppressModeDebounce) return;
            OnPropertyChanged(nameof(CurrentTip));
            if (!string.IsNullOrWhiteSpace(JsonXmlInput))
                DoJsonXmlDebounced();
        }

        private void DoJsonXmlDebounced()
        {
            _debounce?.Stop();
            _debounce?.Dispose();
            _debounce = new System.Timers.Timer(400) { AutoReset = false };
            _debounce.Elapsed += async (_, _) =>
                await MainThread.InvokeOnMainThreadAsync(DoFormatJsonXml);
            _debounce.Start();
        }

        [RelayCommand]
        private void SetJsonMode(string value)
        {
            IsJsonMode = value == "true";
            if (!string.IsNullOrWhiteSpace(JsonXmlInput))
                DoJsonXmlDebounced();
        }

        [RelayCommand]
        private void ClearJsonXmlInput()
        {
            JsonXmlInput = "";
            JsonXmlOutput = "";
            HasJsonXmlError = false;
            JsonXmlError = "";
        }

        [RelayCommand]
        private async Task FormatCurrentAsync()
        {
            if (string.IsNullOrWhiteSpace(JsonXmlInput)) return;

            ConversionResult r = IsJsonMode
                ? _service.FormatJson(JsonXmlInput)
                : _service.FormatXml(JsonXmlInput);

            if (!r.Success) { HasJsonXmlError = true; JsonXmlError = r.Error ?? ""; return; }

            HasJsonXmlError = false; JsonXmlError = "";
            JsonXmlOutput = r.Output;

            await SaveAsync(ConversionType.JsonXml,
                JsonXmlInput[..Math.Min(60, JsonXmlInput.Length)],
                $"Format {(IsJsonMode ? "JSON" : "XML")}");
        }

        private async Task DoFormatJsonXml()
        {
            if (string.IsNullOrWhiteSpace(JsonXmlInput))
            {
                JsonXmlOutput = ""; HasJsonXmlError = false; JsonXmlError = ""; return;
            }

            ConversionResult r;
            ConversionType type;

            if (IsJsonMode)
            {
                r = _service.FormatJson(JsonXmlInput);
                type = ConversionType.JsonXml;
            }
            else
            {
                r = _service.FormatXml(JsonXmlInput);
                type = ConversionType.JsonXml;
            }

            if (!r.Success) { HasJsonXmlError = true; JsonXmlError = r.Error ?? ""; return; }

            HasJsonXmlError = false; JsonXmlError = "";
            JsonXmlOutput = r.Output;
            var outText = r.Output ?? "";
            await SaveAsync(type, JsonXmlInput[..Math.Min(50, JsonXmlInput.Length)], outText[..Math.Min(80, outText.Length)]);
        }

        [RelayCommand]
        private async Task ConvertJsonToXmlAsync()
        {
            var r = _service.JsonToXml(JsonXmlInput);
            if (!r.Success) { HasJsonXmlError = true; JsonXmlError = r.Error ?? ""; return; }

            HasJsonXmlError = false; JsonXmlError = "";
            JsonXmlOutput = r.Output;

            _suppressModeDebounce = true;
            IsJsonMode = false;
            _suppressModeDebounce = false;

            var xmlOut = r.Output ?? "";
            await SaveAsync(ConversionType.JsonXml, "JSON→XML", xmlOut[..Math.Min(80, xmlOut.Length)]);
        }

        [RelayCommand]
        private async Task ConvertXmlToJsonAsync()
        {
            var r = _service.XmlToJson(JsonXmlInput);
            if (!r.Success) { HasJsonXmlError = true; JsonXmlError = r.Error ?? ""; return; }

            HasJsonXmlError = false; JsonXmlError = "";
            JsonXmlOutput = r.Output;

            _suppressModeDebounce = true;
            IsJsonMode = true;
            _suppressModeDebounce = false;

            var jsonOut = r.Output ?? "";
            await SaveAsync(ConversionType.JsonXml, "XML→JSON", jsonOut[..Math.Min(80, jsonOut.Length)]);
        }

        [RelayCommand]
        private async Task EncodeUrlAsync()
        {
            var r = _service.UrlEncode(UrlInput);
            if (!r.Success) { HasUrlError = true; UrlError = r.Error ?? ""; return; }
            HasUrlError = false; UrlError = "";
            UrlOutput = r.Output;
            await SaveAsync(ConversionType.Url, UrlInput, r.Output);
        }

        [RelayCommand]
        private async Task DecodeUrlAsync()
        {
            var r = _service.UrlDecode(UrlInput);
            if (!r.Success) { HasUrlError = true; UrlError = r.Error ?? ""; return; }
            HasUrlError = false; UrlError = "";
            UrlOutput = r.Output;
            await SaveAsync(ConversionType.Url, UrlInput, r.Output);
        }

        partial void OnUrlInputChanged(string value)
        {
            if (value.Contains('%'))
                DoUrlAutoDebounced();
        }

        private void DoUrlAutoDebounced()
        {
            _debounce?.Stop();
            _debounce?.Dispose();
            _debounce = new System.Timers.Timer(400) { AutoReset = false };
            _debounce.Elapsed += async (_, _) =>
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var r = _service.UrlDecode(UrlInput);
                    if (!r.Success) return;
                    HasUrlError = false; UrlError = "";
                    UrlOutput = r.Output;
                    await SaveAsync(ConversionType.Url, UrlInput, r.Output);
                });
            _debounce.Start();
        }

        [RelayCommand]
        private async Task CopyToClipboardAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            await Clipboard.Default.SetTextAsync(text);
        }

        private async Task LoadHistoryAsync()
        {
            var items = await _service.GetHistoryAsync(_profileId, 30);
            var vms = items.Select(h => new HistoryItemVm
            {
                Id = h.Id,
                Type = h.Type,
                TypeIcon = TypeIcon(h.Type),
                TypeLabel = TypeLabel(h.Type),
                Input = h.InputText,
                Output = h.OutputText,
                TimeAgo = FormatTimeAgo(h.CreatedAt)
            }).ToList();

            History = new ObservableCollection<HistoryItemVm>(vms);
            HasHistory = History.Count > 0;
        }

        [RelayCommand]
        private async Task ClearTodayHistoryAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Очистить историю",
                "Удалить все конверсии за сегодня?",
                "Очистить", "Отмена");
            if (!confirm) return;
            await _service.ClearTodayHistoryAsync(_profileId);
            await LoadHistoryAsync();
        }

        [RelayCommand]
        private void RestoreFromHistory(HistoryItemVm item)
        {
            SelectedTabIndex = item.Type switch
            {
                ConversionType.Numeral => 0,
                ConversionType.Color => 1,
                ConversionType.Time => 2,
                ConversionType.JsonXml => 3,
                ConversionType.Url => 4,
                _ => SelectedTabIndex
            };

            switch (item.Type)
            {
                case ConversionType.Numeral: NumeralInput = item.Input; break;
                case ConversionType.Color: ColorInput = item.Input; break;
                case ConversionType.Time: TimeInput = item.Input; break;
                case ConversionType.JsonXml: JsonXmlInput = item.Input; break;
                case ConversionType.Url: UrlInput = item.Input; break;
            }
        }

        private async Task SaveAsync(ConversionType type, string input, string output,
        string? meta = null)
        {
            await _service.SaveToHistoryAsync(_profileId, type, input, output, meta);
            await LoadHistoryAsync();
        }

        private static string TypeIcon(ConversionType t) => t switch
        {
            ConversionType.Numeral => "NUM",
            ConversionType.Color => "CLR",
            ConversionType.Time => "TZ",
            ConversionType.JsonXml => "XML",
            ConversionType.Url => "URL",
            _ => "···"
        };

        private static string TypeLabel(ConversionType t) => t switch
        {
            ConversionType.Numeral => "HEX",
            ConversionType.Color => "Цвет",
            ConversionType.Time => "Время",
            ConversionType.JsonXml => "JSON/XML",
            ConversionType.Url => "URL",
            _ => ""
        };

        private static string FormatTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            return utc.ToLocalTime().ToString("dd MMM");
        }

    }
}
