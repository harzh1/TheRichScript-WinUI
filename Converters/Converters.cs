using System;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace TheRichScript_WinUI.Converters;

public class CurrencyConverter : IValueConverter
{
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Gray);

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value == null) return "₹0.00";

        double amount = 0;
        if (value is double d) amount = d;
        else if (value is float f) amount = f;
        else if (value is int i) amount = i;
        else if (value is long l) amount = l;
        else if (value is decimal dec) amount = (double)dec;
        else if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) amount = parsed;

        var symbol = parameter as string;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            try
            {
                var settingsService = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
                symbol = settingsService?.Settings?.CurrencySymbol ?? "₹";
            }
            catch
            {
                symbol = "₹";
            }
        }

        return $"{symbol} {amount:N2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class AmountColorConverter : IValueConverter
{
    private static readonly SolidColorBrush IncomeBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // #10B981 Emerald Green
    private static readonly SolidColorBrush ExpenseBrush = new(Windows.UI.Color.FromArgb(255, 244, 63, 94)); // #F43F5E Coral Rose
    private static readonly SolidColorBrush TransferBrush = new(Windows.UI.Color.FromArgb(255, 56, 189, 248)); // #38BDF8 Sky Blue

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string str)
        {
            if (string.Equals(str, "INCOME", StringComparison.OrdinalIgnoreCase)) return IncomeBrush;
            if (string.Equals(str, "EXPENSE", StringComparison.OrdinalIgnoreCase)) return ExpenseBrush;
            if (string.Equals(str, "TRANSFER", StringComparison.OrdinalIgnoreCase)) return TransferBrush;
        }
        return TransferBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class TypeToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string str)
        {
            if (string.Equals(str, "INCOME", StringComparison.OrdinalIgnoreCase)) return "\uEAFC"; // Market (Trend Up)
            if (string.Equals(str, "EXPENSE", StringComparison.OrdinalIgnoreCase)) return "\uEF42"; // MarketDown (Trend Down)
            if (string.Equals(str, "TRANSFER", StringComparison.OrdinalIgnoreCase)) return "\uE72A"; // Forward Arrow
        }
        return "\uEF42";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class TypeToBadgeTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string str)
        {
            if (string.Equals(str, "INCOME", StringComparison.OrdinalIgnoreCase)) return "↗ INCOME";
            if (string.Equals(str, "EXPENSE", StringComparison.OrdinalIgnoreCase)) return "↘ EXPENSE";
            if (string.Equals(str, "TRANSFER", StringComparison.OrdinalIgnoreCase)) return "→ TRANSFER";
        }
        return "↘ EXPENSE";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class TypeToForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush IncomeBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // #10B981
    private static readonly SolidColorBrush ExpenseBrush = new(Windows.UI.Color.FromArgb(255, 244, 63, 94)); // #F43F5E
    private static readonly SolidColorBrush TransferBrush = new(Windows.UI.Color.FromArgb(255, 56, 189, 248)); // #38BDF8
    private static readonly SolidColorBrush DefaultBrush = new(Windows.UI.Color.FromArgb(255, 107, 114, 128));

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string str)
        {
            if (string.Equals(str, "INCOME", StringComparison.OrdinalIgnoreCase)) return IncomeBrush;
            if (string.Equals(str, "EXPENSE", StringComparison.OrdinalIgnoreCase)) return ExpenseBrush;
            if (string.Equals(str, "TRANSFER", StringComparison.OrdinalIgnoreCase)) return TransferBrush;
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class TypeToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush IncomeBgBrush = new(Windows.UI.Color.FromArgb(26, 16, 185, 129)); // #1A10B981 (10% tint)
    private static readonly SolidColorBrush ExpenseBgBrush = new(Windows.UI.Color.FromArgb(26, 244, 63, 94)); // #1AF43F5E (10% tint)
    private static readonly SolidColorBrush TransferBgBrush = new(Windows.UI.Color.FromArgb(26, 56, 189, 248)); // #1A38BDF8 (10% tint)
    private static readonly SolidColorBrush DefaultBgBrush = new(Windows.UI.Color.FromArgb(25, 128, 128, 128));

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string str)
        {
            if (string.Equals(str, "INCOME", StringComparison.OrdinalIgnoreCase)) return IncomeBgBrush;
            if (string.Equals(str, "EXPENSE", StringComparison.OrdinalIgnoreCase)) return ExpenseBgBrush;
            if (string.Equals(str, "TRANSFER", StringComparison.OrdinalIgnoreCase)) return TransferBgBrush;
        }
        return DefaultBgBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class PinToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool b && b ? "\uE840" : "\uE718";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class PercentTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is double d)
        {
            return $"{d:F1}%";
        }
        if (value is float f)
        {
            return $"{f:F1}%";
        }
        return "0.0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var rawParam = parameter as string ?? "dd MMM yyyy";
        var prefix = "";
        var format = rawParam;

        if (rawParam.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "Target: ";
            format = "dd MMM yyyy";
        }
        else if (rawParam.StartsWith("Due:", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "Due: ";
            format = "dd MMM yyyy";
        }

        if (value is DateTime dt)
        {
            return $"{prefix}{dt.ToString(format, CultureInfo.CurrentCulture)}";
        }
        if (value is DateTimeOffset dto)
        {
            return $"{prefix}{dto.ToString(format, CultureInfo.CurrentCulture)}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isVisible = false;

        if (value is bool b)
        {
            isVisible = b;
        }
        else if (value is int i)
        {
            isVisible = i > 0;
        }
        else if (value is long l)
        {
            isVisible = l > 0;
        }
        else if (value is double d)
        {
            isVisible = Math.Abs(d) > 0.0001;
        }
        else if (value != null)
        {
            isVisible = true;
        }

        if (parameter is string paramStr && string.Equals(paramStr, "Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isVisible = false;
        if (value is bool b) isVisible = b;
        else if (value is int i) isVisible = i > 0;
        else if (value != null) isVisible = true;

        return isVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Collapsed;
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool b)
        {
            return !b;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var isNull = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        if (parameter is string paramStr && paramStr == "Inverse")
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ProgressColorConverter : IValueConverter
{
    private static readonly SolidColorBrush AccentBrush = new(Windows.UI.Color.FromArgb(255, 96, 205, 255)); // Standard Windows Accent Light Blue

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var progress = 0.0;
        if (value is double d) progress = d;
        else if (value is float f) progress = f;

        if (parameter is string p && p == "PercentText")
        {
            return $"{progress:F1}%";
        }

        return AccentBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var hasValue = value != null && !string.IsNullOrWhiteSpace(value.ToString());
        if (parameter is string p && p == "Inverse")
        {
            return !hasValue;
        }
        return hasValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
