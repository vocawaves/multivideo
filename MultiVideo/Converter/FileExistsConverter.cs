using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace MultiVideo.Converter;

public class FileExistsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && targetType == typeof(bool))
        {
            return !File.Exists(path);
        }
        
        throw new InvalidDataException();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        //throw new NotImplementedException();
        return null;
    }
}