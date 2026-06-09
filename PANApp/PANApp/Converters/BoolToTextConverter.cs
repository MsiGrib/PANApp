using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PANApp.Converters;

public class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string param || string.IsNullOrEmpty(param))
            return value?.ToString();

        var parts = param.Split('|');
        var trueText = parts.Length > 0 ? parts[0] : string.Empty;
        var falseText = parts.Length > 1 ? parts[1] : string.Empty;

        var boolValue = value is bool b && b;

        return boolValue ? trueText : falseText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}