using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace MultiVideo.Converter;

public class TimeSpanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts && targetType == typeof(string))
        {
            return ts.TotalMilliseconds.ToString(culture);
        }
        else if (value is TimeSpan ts2 && targetType == typeof(double))
        {
            return ts2.TotalMilliseconds;
        }
        throw new InvalidDataException();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isDouble = double.TryParse(value?.ToString(), culture, out var dVal);
        if (isDouble)
        {
            return TimeSpan.FromMilliseconds(dVal);
        }
        return TimeSpan.Zero;
    }
}