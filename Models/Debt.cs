using System;
using System.Text.Json.Serialization;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Models;

public class Debt
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "LENT"; // LENT, BORROWED

    [JsonPropertyName("personName")]
    public string PersonName { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Amount { get; set; }

    [JsonPropertyName("settledAmount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double SettledAmount { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, SETTLED, EXPIRED

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("contactId")]
    public string? ContactId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [JsonIgnore]
    public bool IsLent => string.Equals(Type, "LENT", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsBorrowed => string.Equals(Type, "BORROWED", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsSettled => string.Equals(Status, "SETTLED", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsExpired => string.Equals(Status, "EXPIRED", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public double RemainingAmount => Math.Max(0, Amount - SettledAmount);

    [JsonIgnore]
    public double SettlementPercentage => Amount > 0 ? Math.Min(100.0, (SettledAmount / Amount) * 100.0) : 0.0;

    [JsonIgnore]
    public string CurrencySymbol => CountryCurrencyData.GetCurrencySymbol(Currency);

    [JsonIgnore]
    public string FormattedAmount => $"{CurrencySymbol}{Amount:N2}";

    [JsonIgnore]
    public string FormattedSettledAmount => $"{CurrencySymbol}{SettledAmount:N2}";

    [JsonIgnore]
    public string FormattedRemaining => $"{CurrencySymbol}{RemainingAmount:N2}";

    [JsonIgnore]
    public double BaseAmount { get; set; }

    [JsonIgnore]
    public double BaseRemainingAmount { get; set; }

    [JsonIgnore]
    public string? BaseCurrency { get; set; }

    [JsonIgnore]
    public string FormattedBaseRemaining => $"≈ {CountryCurrencyData.GetCurrencySymbol(BaseCurrency)}{BaseRemainingAmount:N2}";

    [JsonIgnore]
    public bool ShowBaseValue => !string.IsNullOrWhiteSpace(BaseCurrency) &&
                                 !string.IsNullOrWhiteSpace(Currency) &&
                                 !string.Equals(Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string FormattedSettlement => $"{CurrencySymbol}{SettledAmount:N2} of {CurrencySymbol}{Amount:N2} settled";

    [JsonIgnore]
    public string TypeGlyph => IsLent ? "\uE896" : "\uE898";

    [JsonIgnore]
    public string StatusBadgeText => IsSettled ? "SETTLED" : (IsExpired ? "WRITTEN OFF" : "ACTIVE");

    [JsonIgnore]
    public string DueDateText => DueDate.HasValue ? $"Due {DueDate.Value:dd MMM yyyy}" : "No due date";
}

public class CreateDebtRequest
{
    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "LENT";

    [JsonPropertyName("personName")]
    public string PersonName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("contactId")]
    public string? ContactId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class UpdateDebtRequest
{
    [JsonPropertyName("assetId")]
    public string? AssetId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("personName")]
    public string? PersonName { get; set; }

    [JsonPropertyName("amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("contactId")]
    public string? ContactId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class SettleDebtRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("settleAssetAmount")]
    public double? SettleAssetAmount { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }
}
