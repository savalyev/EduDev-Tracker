using EduDev_Tracker.Features.Analytics.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    public class PeriodStyleConverter : IValueConverter
    {
        private static readonly Style ActiveStyle = new(typeof(Button))
        {
            Setters =
        {
            new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#2DD4BF") },
            new Setter { Property = Button.TextColorProperty,       Value = Color.FromArgb("#0B1120") },
            new Setter { Property = Button.FontSizeProperty,        Value = 13.0 },
            new Setter { Property = Button.FontAttributesProperty,  Value = FontAttributes.Bold },
            new Setter { Property = Button.CornerRadiusProperty,    Value = 20 },
            new Setter { Property = Button.HeightRequestProperty,   Value = 40.0 },
            new Setter { Property = Button.PaddingProperty,         Value = new Thickness(20, 0) },
        }
        };

        private static readonly Style NormalStyle = new(typeof(Button))
        {
            Setters =
        {
            new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#1E293B") },
            new Setter { Property = Button.TextColorProperty,       Value = Color.FromArgb("#CBD5E1") },
            new Setter { Property = Button.FontSizeProperty,        Value = 13.0 },
            new Setter { Property = Button.FontAttributesProperty,  Value = FontAttributes.None },
            new Setter { Property = Button.CornerRadiusProperty,    Value = 20 },
            new Setter { Property = Button.HeightRequestProperty,   Value = 40.0 },
            new Setter { Property = Button.PaddingProperty,         Value = new Thickness(20, 0) },
        }
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not AnalyticsPeriod current) return NormalStyle;

            var target = parameter?.ToString() switch
            {
                "Week" => AnalyticsPeriod.Week,
                "Month" => AnalyticsPeriod.Month,
                "Custom" => AnalyticsPeriod.Custom,
                _ => AnalyticsPeriod.Week
            };

            return current == target ? ActiveStyle : NormalStyle;
        }

        public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class BoolToFormatStyleConverter : IValueConverter
    {
        private static readonly Style ActiveStyle = new(typeof(Button))
        {
            Setters =
        {
            new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#2DD4BF") },
            new Setter { Property = Button.TextColorProperty,       Value = Color.FromArgb("#0B1120") },
            new Setter { Property = Button.FontSizeProperty,        Value = 14.0 },
            new Setter { Property = Button.FontAttributesProperty,  Value = FontAttributes.Bold },
            new Setter { Property = Button.CornerRadiusProperty,    Value = 12 },
            new Setter { Property = Button.HeightRequestProperty,   Value = 48.0 },
            new Setter { Property = Button.WidthRequestProperty,    Value = 90.0 },
        }
        };

        private static readonly Style NormalStyle = new(typeof(Button))
        {
            Setters =
        {
            new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#1E293B") },
            new Setter { Property = Button.TextColorProperty,       Value = Color.FromArgb("#CBD5E1") },
            new Setter { Property = Button.FontSizeProperty,        Value = 14.0 },
            new Setter { Property = Button.FontAttributesProperty,  Value = FontAttributes.Bold },
            new Setter { Property = Button.CornerRadiusProperty,    Value = 12 },
            new Setter { Property = Button.HeightRequestProperty,   Value = 48.0 },
            new Setter { Property = Button.WidthRequestProperty,    Value = 90.0 },
        }
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? ActiveStyle : NormalStyle;

        public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class PeriodColorConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            var isActive = v?.ToString() == p?.ToString();
            return isActive ? Color.FromArgb("#2DD4BF") : Color.FromArgb("#1E293B");
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Период → цвет текста кнопки ──────────────────────────────────────────
    public class PeriodTextColorConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            var isActive = v?.ToString() == p?.ToString();
            return isActive ? Color.FromArgb("#0B1120") : Color.FromArgb("#CBD5E1");
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Период → FontAttributes ───────────────────────────────────────────────
    public class PeriodFontConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            var isActive = v?.ToString() == p?.ToString();
            return isActive ? FontAttributes.Bold : FontAttributes.None;
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Модуль → цвет фона Border ────────────────────────────────────────────
    public class ModuleColorConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            var isActive = v?.ToString() == p?.ToString();
            return isActive ? Color.FromArgb("#1E3A2F") : Color.FromArgb("#1E293B");
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Модуль → цвет текста ─────────────────────────────────────────────────
    public class ModuleTextColorConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            var isActive = v?.ToString() == p?.ToString();
            return isActive ? Color.FromArgb("#2DD4BF") : Color.FromArgb("#94A3B8");
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
