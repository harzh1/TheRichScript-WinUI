using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TheRichScript_WinUI.Models;

public partial class PlannedExpense : ObservableObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyName("assetId")]
    private string? assetId = string.Empty;

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }

    [ObservableProperty]
    [property: JsonPropertyName("name")]
    private string name = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyName("category")]
    private string category = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyName("amount")]
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    private double amount;

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

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [ObservableProperty]
    [property: JsonPropertyName("sourceAssetName")]
    private string? sourceAssetName;

    [ObservableProperty]
    [property: JsonPropertyName("targetAssetName")]
    private string? targetAssetName;

    [JsonIgnore]
    public bool IsRecurring => string.Equals(Type, "RECURRING", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsIncome => string.Equals(TxType, "INCOME", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsTransfer => string.Equals(TxType, "TRANSFER", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(TargetAssetId);

    [JsonIgnore]
    public bool IsSubscription => IsRecurring && !IsTransfer && !IsIncome;

    [JsonIgnore]
    public string DisplayFrequency => Frequency ?? "One-time";

    [JsonIgnore]
    public string TransferFlowDisplay => !string.IsNullOrEmpty(SourceAssetName) && !string.IsNullOrEmpty(TargetAssetName)
        ? $"{SourceAssetName} ➔ {TargetAssetName}"
        : (!string.IsNullOrEmpty(SourceAssetName) ? $"{SourceAssetName} ➔ Target Account" : "Transfer");

    [JsonIgnore]
    public string SourceAccountDisplay => !string.IsNullOrEmpty(SourceAssetName) ? $"Paid from: {SourceAssetName}" : "Source Account";

    [JsonIgnore]
    public bool IsDueNow => NextDueDate.HasValue && NextDueDate.Value.Date <= DateTime.Today;

    [JsonIgnore]
    public string DueStatusText => NextDueDate.HasValue
        ? (NextDueDate.Value.Date < DateTime.Today ? "Overdue" : (NextDueDate.Value.Date == DateTime.Today ? "Due Today" : "Scheduled"))
        : "Not scheduled";
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
