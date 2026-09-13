using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("monthlyLimit")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? MonthlyLimit { get; set; }

    [JsonPropertyName("yearlyLimit")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? YearlyLimit { get; set; }

    [JsonPropertyName("limitId")]
    public string? LimitId { get; set; }

    [JsonIgnore]
    public double MonthlySpent { get; set; }

    [JsonIgnore]
    public bool HasMonthlyLimit => MonthlyLimit.HasValue && MonthlyLimit.Value > 0;

    [JsonIgnore]
    public double LimitPercentage => HasMonthlyLimit ? (MonthlySpent / MonthlyLimit!.Value) * 100.0 : 0;

    [JsonIgnore]
    public string FormattedSpentVsLimit
    {
        get
        {
            if (!HasMonthlyLimit) return "No limit set";
            var symbol = "₹";
            try
            {
                var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
                if (settings?.Settings?.CurrencySymbol != null)
                {
                    symbol = settings.Settings.CurrencySymbol;
                }
            }
            catch { }
            return $"{symbol} {MonthlySpent:N2} / {symbol} {MonthlyLimit!.Value:N2}";
        }
    }

    [JsonIgnore]
    public string LimitStatusText => HasMonthlyLimit 
        ? (MonthlySpent > MonthlyLimit!.Value ? "OVER LIMIT" : (LimitPercentage >= 80 ? "NEAR LIMIT" : "ON TRACK"))
        : string.Empty;

    [JsonIgnore]
    public string FormattedLimits
    {
        get
        {
            var symbol = "₹";
            try
            {
                var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
                if (settings?.Settings?.CurrencySymbol != null)
                {
                    symbol = settings.Settings.CurrencySymbol;
                }
            }
            catch { }

            if (MonthlyLimit.HasValue && YearlyLimit.HasValue)
                return $"Monthly: {symbol} {MonthlyLimit.Value:N0} | Yearly: {symbol} {YearlyLimit.Value:N0}";
            if (MonthlyLimit.HasValue)
                return $"Monthly: {symbol} {MonthlyLimit.Value:N0}";
            if (YearlyLimit.HasValue)
                return $"Yearly: {symbol} {YearlyLimit.Value:N0}";
            return "No budget limits set";
        }
    }
}

public class CreateCategoryRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("monthlyLimit")]
    public double? MonthlyLimit { get; set; }

    [JsonPropertyName("yearlyLimit")]
    public double? YearlyLimit { get; set; }
}

public class UpdateCategoryRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("monthlyLimit")]
    public double? MonthlyLimit { get; set; }

    [JsonPropertyName("yearlyLimit")]
    public double? YearlyLimit { get; set; }
}
