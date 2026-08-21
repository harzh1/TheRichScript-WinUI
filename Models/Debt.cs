using System;
using System.Text.Json.Serialization;

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

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("settledAmount")]
    public double SettledAmount { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, SETTLED, EXPIRED

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

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
    public string FormattedSettlement
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
            return $"{symbol} {SettledAmount:N2} of {symbol} {Amount:N2} settled";
        }
    }

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

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }
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

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }
}

public class SettleDebtRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;
}
