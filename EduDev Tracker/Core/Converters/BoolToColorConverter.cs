using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.FromArgb("#2DD4BF");
        public Color FalseColor { get; set; } = Color.FromArgb("#1E293B");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string p && p == "Selected")
            {
                bool sel = value is bool b && b;
                return sel ? Color.FromArgb("#0D2A2E") : Color.FromArgb("#111827");
            }
            bool flag = value is bool bv && bv;
            return flag ? TrueColor : FalseColor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class BoolToColorConverterForMain : IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new();

        public object Convert(object? value, Type t, object? parameter, CultureInfo c)
        {
            var parts = parameter?.ToString()?.Split('|') ?? [];
            if (parts.Length < 2) return Colors.Transparent;
            var hex = value is true ? parts[0] : parts[1];
            return Color.FromArgb(hex);
        }

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
