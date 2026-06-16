using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EduDev_Tracker.Core.Converters
{
    public class RatioToHeightConverter : IValueConverter
    {
        public static readonly RatioToHeightConverter Instance = new();

        public object Convert(object? value, Type t, object? parameter, CultureInfo c)
        {
            var ratio = value is double d ? d : 0;
            var maxH = double.TryParse(parameter?.ToString(), out var p) ? p : 100;
            var result = Math.Max(4, ratio * maxH);
            return result;
        }

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
