using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    public class PhaseToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var col = value is PomodoroPhase phase ? phase switch
            {
                PomodoroPhase.Work => Color.FromArgb("#2DD4BF"),
                PomodoroPhase.ShortBreak => Color.FromArgb("#60A5FA"),
                PomodoroPhase.LongBreak => Color.FromArgb("#A78BFA"),
                _ => Color.FromArgb("#2DD4BF")
            } : Color.FromArgb("#2DD4BF");

            return new SolidColorBrush(col);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
