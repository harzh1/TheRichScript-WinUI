using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class Goal
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("targetAmount")]
    public double TargetAmount { get; set; }

    [JsonPropertyName("currentAmount")]
    public double CurrentAmount { get; set; }

    [JsonPropertyName("expectedDate")]
    public DateTime ExpectedDate { get; set; }

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("recurringAmount")]
    public double? RecurringAmount { get; set; }

    [JsonPropertyName("recurringFrequency")]
    public string? RecurringFrequency { get; set; }

    [JsonPropertyName("sourceAssetId")]
    public string? SourceAssetId { get; set; }

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }

    [JsonPropertyName("imageUrls")]
    public List<string> ImageUrls { get; set; } = new();

    [JsonPropertyName("lastFundedAt")]
    public DateTime? LastFundedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public double ProgressPercentage => TargetAmount > 0 ? Math.Min(100.0, (CurrentAmount / TargetAmount) * 100.0) : 0.0;

    [JsonIgnore]
    public double RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);

    [JsonIgnore]
    public bool IsCompleted => CurrentAmount >= TargetAmount;

    [JsonIgnore]
    public string? PrimaryImageUrl => ImageUrls.Count > 0 ? ImageUrls[0] : null;

    [JsonIgnore]
    public string FormattedProgress => $"{CurrentAmount:N0} / {TargetAmount:N0} ({ProgressPercentage:F1}%)";
}

public class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public double TargetAmount { get; set; }
    public double InitialDeposit { get; set; }
    public string? AssetId { get; set; }
    public DateTime ExpectedDate { get; set; } = DateTime.Now.AddMonths(6);
    public double? RecurringAmount { get; set; }
    public string? RecurringFrequency { get; set; }
    public string? SourceAssetId { get; set; }
    public string? TargetAssetId { get; set; }
    public List<string> ImageFilePaths { get; set; } = new();
}

public class UpdateGoalRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("targetAmount")]
    public double? TargetAmount { get; set; }

    [JsonPropertyName("expectedDate")]
    public DateTime? ExpectedDate { get; set; }

    [JsonPropertyName("recurringAmount")]
    public double? RecurringAmount { get; set; }

    [JsonPropertyName("recurringFrequency")]
    public string? RecurringFrequency { get; set; }

    [JsonPropertyName("isPinned")]
    public bool? IsPinned { get; set; }

    [JsonPropertyName("sourceAssetId")]
    public string? SourceAssetId { get; set; }

    [JsonPropertyName("targetAssetId")]
    public string? TargetAssetId { get; set; }
}

public class AddGoalFundsRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;
}

public class DeleteGoalRequest
{
    [JsonPropertyName("refundAssetId")]
    public string? RefundAssetId { get; set; }
}
