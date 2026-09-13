using System;

namespace TheRichScript_WinUI.Models;

public class CountryInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;

    public string DisplayName => $"{FlagEmoji} {Name} ({Code})";

    public override string ToString() => DisplayName;

    public static string GetFlagEmoji(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) return "🌐";
        countryCode = countryCode.ToUpperInvariant();
        if (countryCode[0] < 'A' || countryCode[0] > 'Z' || countryCode[1] < 'A' || countryCode[1] > 'Z') return "🌐";
        int first = countryCode[0] - 'A' + 0x1F1E6;
        int second = countryCode[1] - 'A' + 0x1F1E6;
        return char.ConvertFromUtf32(first) + char.ConvertFromUtf32(second);
    }
}

public class CurrencyInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    public string DisplayName => $"{Code} - {Symbol} ({Name})";

    public override string ToString() => DisplayName;
}
