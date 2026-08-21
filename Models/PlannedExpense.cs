using System;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class PlannedExpense
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "RECURRING"; // MANUAL, RECURRING

    [JsonPropertyName("txType")]
    public string TxType { get; set; } = "EXPENSE"; // EXPENSE, INCOME

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; } // DAILY, WEEKLY, MONTHLY, YEARLY

    [JsonPropertyName("nextDueDate")]
    public DateTime? NextDueDate { get; set; }

    [JsonPropertyName("lastFundedAt")]
    public DateTime? LastFundedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public bool IsRecurring => string.Equals(Type, "RECURRING", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsIncome => string.Equals(TxType, "INCOME", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string DisplayFrequency => Frequency ?? "One-time";
}

public class CreatePlanRequest
{
    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "RECURRING";

    [JsonPropertyName("txType")]
    public string TxType { get; set; } = "EXPENSE";

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    [JsonPropertyName("nextDueDate")]
    public DateTime? NextDueDate { get; set; }
}

public class UpdatePlanRequest
{
    [JsonPropertyName("assetId")]
    public string? AssetId { get; set; }

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("txType")]
    public string? TxType { get; set; }

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    [JsonPropertyName("nextDueDate")]
    public DateTime? NextDueDate { get; set; }
}
