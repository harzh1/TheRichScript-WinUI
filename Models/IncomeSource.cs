using System;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class IncomeSource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "ACTIVE"; // ACTIVE or PASSIVE

    [JsonPropertyName("isFixed")]
    public bool IsFixed { get; set; } = true;

    [JsonPropertyName("expectedAmount")]
    public double? ExpectedAmount { get; set; }

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; } // DAILY, WEEKLY, MONTHLY, YEARLY

    [JsonPropertyName("defaultAssetId")]
    public string? DefaultAssetId { get; set; }

    [JsonPropertyName("defaultAssetName")]
    public string? DefaultAssetName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("totalReceived")]
    public double TotalReceived { get; set; }

    [JsonPropertyName("transactionCount")]
    public int TransactionCount { get; set; }

    [JsonPropertyName("lastReceivedDate")]
    public DateTime? LastReceivedDate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    // UI Helpers
    [JsonIgnore]
    public string DisplayType => Type == "PASSIVE" ? "PASSIVE" : "ACTIVE";

    [JsonIgnore]
    public string DisplayNature => IsFixed ? "Fixed" : "Variable";

    [JsonIgnore]
    public string DisplayFrequency => Frequency switch
    {
        "DAILY" => "Daily",
        "WEEKLY" => "Weekly",
        "MONTHLY" => "Monthly",
        "YEARLY" => "Yearly",
        _ => "Flexible"
    };

    [JsonIgnore]
    public string TypeBadgeBackground => Type == "PASSIVE" ? "#1A6366F1" : "#1A10B981";

    [JsonIgnore]
    public string TypeBadgeForeground => Type == "PASSIVE" ? "#6366F1" : "#10B981";

    [JsonIgnore]
    public string TypeIconGlyph => Type == "PASSIVE" ? "\uE8D4" : "\uEAFC";

    [JsonIgnore]
    public string TypeIconBackground => Type == "PASSIVE" ? "#1A6366F1" : "#1A10B981";

    [JsonIgnore]
    public string TypeIconForeground => Type == "PASSIVE" ? "#6366F1" : "#10B981";

    [JsonIgnore]
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconUrl);
}

public class CreateIncomeSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "ACTIVE";
    public bool IsFixed { get; set; } = true;
    public double? ExpectedAmount { get; set; }
    public string? Frequency { get; set; }
    public string? DefaultAssetId { get; set; }
    public string? Description { get; set; }
    public string? IconFilePath { get; set; }
}

public class UpdateIncomeSourceRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool? IsFixed { get; set; }
    public double? ExpectedAmount { get; set; }
    public string? Frequency { get; set; }
    public string? DefaultAssetId { get; set; }
    public string? Description { get; set; }
    public string? IconFilePath { get; set; }
    public bool? IsActive { get; set; }
}

public class IncomeSourcesSummary
{
    [JsonPropertyName("totalActiveExpectedMonthly")]
    public double TotalActiveExpectedMonthly { get; set; }

    [JsonPropertyName("totalPassiveExpectedMonthly")]
    public double TotalPassiveExpectedMonthly { get; set; }

    [JsonPropertyName("totalExpectedMonthly")]
    public double TotalExpectedMonthly { get; set; }

    [JsonPropertyName("totalReceivedThisMonth")]
    public double TotalReceivedThisMonth { get; set; }

    [JsonPropertyName("totalReceivedThisYear")]
    public double TotalReceivedThisYear { get; set; }

    [JsonPropertyName("totalSourcesCount")]
    public int TotalSourcesCount { get; set; }

    [JsonPropertyName("activeSourcesCount")]
    public int ActiveSourcesCount { get; set; }

    [JsonPropertyName("passiveSourcesCount")]
    public int PassiveSourcesCount { get; set; }
}
