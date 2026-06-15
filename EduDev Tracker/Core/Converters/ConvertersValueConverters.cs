using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    public class TabColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int selected && parameter is string paramStr
                && int.TryParse(paramStr, out int tabIndex))
            {
                return selected == tabIndex
                    ? Color.FromArgb("#1E3A2F")   // активный — тёмный teal
                    : Color.FromArgb("#111827");  // неактивный
            }
            return Color.FromArgb("#111827");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TabTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int selected && parameter is string paramStr
                && int.TryParse(paramStr, out int tabIndex))
            {
                return selected == tabIndex
                    ? Color.FromArgb("#2DD4BF")
                    : Color.FromArgb("#64748B");
            }
            return Color.FromArgb("#64748B");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TabVisibleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int selected && parameter is string paramStr
                && int.TryParse(paramStr, out int tabIndex))
                return selected == tabIndex;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;
    }

    /// <summary>
    /// BoolToStringConverter: ConverterParameter="ТрутВариант|ФолсВариант"
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|') ?? [];
            if (parts.Length < 2) return "";
            return value is bool b && b ? parts[0] : parts[1];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
