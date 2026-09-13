using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Models;

public class Asset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("typeId")]
    public string TypeId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("value")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Value { get; set; }

    [JsonPropertyName("quantity")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Quantity { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [JsonIgnore]
    public string CurrencySymbol => CountryCurrencyData.GetCurrencySymbol(Currency);

    [JsonIgnore]
    public string FormattedValue => $"{CurrencySymbol}{Value:N2}";

    [JsonIgnore]
    public string DisplayNameWithBalance => $"{Name} ({CurrencySymbol}{Value:N2})";

    [JsonIgnore]
    public double BaseValue { get; set; }

    [JsonIgnore]
    public string? BaseCurrency { get; set; }

    [JsonIgnore]
    public string FormattedBaseValue => $"≈ {CountryCurrencyData.GetCurrencySymbol(BaseCurrency)}{BaseValue:N2}";

    [JsonIgnore]
    public bool ShowBaseValue => !string.IsNullOrWhiteSpace(BaseCurrency) &&
                                 !string.IsNullOrWhiteSpace(Currency) &&
                                 !string.Equals(Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public double PercentageOfTotal { get; set; }

    [JsonIgnore]
    public string FormattedPercentage => $"{PercentageOfTotal:0.1}% of portfolio";

    [JsonIgnore]
    public string TypeGlyph => (Type ?? string.Empty).ToUpperInvariant() switch
    {
        "BANK" or "SAVINGS" or "CHECKING" => "\uE825",
        "INVESTMENT" or "STOCKS" or "MUTUAL FUND" or "MUTUAL FUNDS" or "EQUITY" => "\uEAFC",
        "CRYPTO" or "CRYPTO CURRENCY" or "BITCOIN" => "\uE945",
        "CASH" or "LIQUID CASH" or "WALLET" => "\uE1D0",
        "REAL ESTATE" or "PROPERTY" or "LAND" => "\uE80F",
        "METALS" or "METAL" or "GOLD" or "SILVER" or "PRECIOUS METAL" or "PRECIOUS METALS" => "\uE734",
        "RETIREMENT" or "EPF" or "PPF" or "PENSION" => "\uE819",
        "VEHICLE" => "\uE806",
        _ => "\uE77B"
    };
}

public class CreateAssetRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("quantity")]
    public double? Quantity { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public class UpdateAssetRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("quantity")]
    public double? Quantity { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public class TransferAssetRequest
{
    [JsonPropertyName("fromAssetId")]
    public string FromAssetId { get; set; } = string.Empty;

    [JsonPropertyName("toAssetId")]
    public string ToAssetId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("destinationAmount")]
    public double? DestinationAmount { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.Now;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
