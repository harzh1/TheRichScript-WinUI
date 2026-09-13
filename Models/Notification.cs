using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class NotificationMeta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

public class AppNotification
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SYSTEM"; // DEBT_DUE, GOAL_MILESTONE, BUDGET_EXCEEDED, PLANNED_EXPENSE_DUE, SYSTEM

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public string TypeGlyph => (Type ?? string.Empty).ToUpperInvariant() switch
    {
        "DEBT_DUE" => "\uE896",
        "GOAL_MILESTONE" => "\uF133",
        "BUDGET_EXCEEDED" => "\uE7BA",
        "PLANNED_EXPENSE_DUE" => "\uE823",
        _ => "\uEA8F"
    };

    [JsonIgnore]
    public string TypeBadgeColor => (Type ?? string.Empty).ToUpperInvariant() switch
    {
        "DEBT_DUE" => "#F59E0B",
        "GOAL_MILESTONE" => "#10B981",
        "BUDGET_EXCEEDED" => "#EF4444",
        "PLANNED_EXPENSE_DUE" => "#6366F1",
        _ => "#6B7280"
    };

    [JsonIgnore]
    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt.ToUniversalTime();
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return CreatedAt.ToString("dd MMM");
        }
    }
}

public class NotificationListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<AppNotification> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public NotificationMeta? Meta { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
