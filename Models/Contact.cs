using System;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class ContactCount
{
    [JsonPropertyName("debts")]
    public int Debts { get; set; }
}

public class Contact
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("_count")]
    public ContactCount? Count { get; set; }

    [JsonIgnore]
    public int DebtsCount => Count?.Debts ?? 0;

    [JsonIgnore]
    public string DisplayPhone => !string.IsNullOrWhiteSpace(Phone) ? Phone : "No phone";

    [JsonIgnore]
    public string DisplayEmail => !string.IsNullOrWhiteSpace(Email) ? Email : "No email";

    [JsonIgnore]
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "?";
            var parts = Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}

public class CreateContactRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class UpdateContactRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
