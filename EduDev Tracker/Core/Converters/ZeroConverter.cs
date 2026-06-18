using System.Globalization;

namespace EduDev_Tracker.Core.Converters
{
    // true когда значение равно 0 — используется для показа пустого состояния списков
    public class ZeroConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i ? i == 0 : value is null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
