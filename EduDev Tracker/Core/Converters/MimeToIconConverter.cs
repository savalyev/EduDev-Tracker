using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    // ════════════════════════════════════════════════════════════════
    //  MimeToIconConverter
    //  NoteAttachment.MimeType → эмодзи-иконка
    //  Использование в XAML:
    //    <converters:MimeToIconConverter x:Key="MimeToIconConverter"/>
    //    Text="{Binding MimeType, Converter={StaticResource MimeToIconConverter}}"
    // ════════════════════════════════════════════════════════════════
    public class MimeToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var mime = value as string ?? string.Empty;

            if (mime.StartsWith("image/")) return "🖼️";
            if (mime.StartsWith("video/")) return "🎬";
            if (mime.StartsWith("audio/")) return "🎵";
            if (mime == "application/pdf") return "📄";
            if (mime.Contains("word") ||
                mime.Contains("document")) return "📝";
            if (mime.Contains("sheet") ||
                mime.Contains("excel") ||
                mime.Contains("csv")) return "📊";
            if (mime.Contains("presentation") ||
                mime.Contains("powerpoint")) return "📑";
            if (mime == "application/zip" ||
                mime == "application/x-rar-compressed" ||
                mime == "application/x-7z-compressed") return "🗜️";
            if (mime.StartsWith("text/")) return "📃";
            if (mime.Contains("json") ||
                mime.Contains("xml")) return "🔧";

            return "📎"; // по умолчанию
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ════════════════════════════════════════════════════════════════
    //  FileSizeConverter
    //  long? (байты) → читаемый размер: "1.2 МБ", "340 КБ", "512 Б"
    //  Использование в XAML:
    //    <converters:FileSizeConverter x:Key="FileSizeConverter"/>
    //    Text="{Binding SizeBytes, Converter={StaticResource FileSizeConverter}}"
    // ════════════════════════════════════════════════════════════════
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return "—";

            long bytes = value switch
            {
                long l => l,
                int i => i,
                double d => (long)d,
                _ => 0L
            };

            if (bytes < 0) return "—";
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} КБ";
            if (bytes < 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} МБ";

            return $"{bytes / (1024.0 * 1024 * 1024):F2} ГБ";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ════════════════════════════════════════════════════════════════
    //  InverseBoolConverter
    //  bool → !bool
    //  Использование в XAML:
    //    IsVisible="{Binding MobileShowEditor,
    //                Converter={StaticResource InverseBoolConverter}}"
    // ════════════════════════════════════════════════════════════════
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;
    }

    // ════════════════════════════════════════════════════════════════
    //  NullToBoolConverter
    //  null → false, not null → true
    //  Использование в XAML:
    //    IsVisible="{Binding CategoryId,
    //                Converter={StaticResource NullToBoolConverter}}"
    // ════════════════════════════════════════════════════════════════
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is not null && (value is not int i || i != 0);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ════════════════════════════════════════════════════════════════
    //  ActiveCategoryColorConverter
    //  Подсвечивает выбранный чип категории
    //  Использование в XAML:
    //    BackgroundColor="{Binding SelectedCategory,
    //                     Converter={StaticResource ActiveCategoryColorConverter},
    //                     ConverterParameter={Binding .}}"
    // ════════════════════════════════════════════════════════════════
    public class ActiveCategoryColorConverter : IValueConverter
    {
        private static readonly Color Active = Color.FromArgb("#2A4A8A");
        private static readonly Color Inactive = Color.FromArgb("#1C2333");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null && parameter is null) return Active;   // "Все" активно
            if (value is null || parameter is null) return Inactive;
            return value.Equals(parameter) ? Active : Inactive;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
