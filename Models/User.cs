using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = "IN";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonIgnore]
    public string FullName
    {
        get
        {
            var first = FirstName?.Trim() ?? string.Empty;
            var last = LastName?.Trim() ?? string.Empty;
            var full = $"{first} {last}".Trim();
            if (!string.IsNullOrWhiteSpace(full)) return full;
            if (!string.IsNullOrWhiteSpace(Email)) return Email.Split('@')[0];
            return "Personal User";
        }
    }

    [JsonIgnore]
    public string DisplayEmail => !string.IsNullOrWhiteSpace(Email) ? Email : "user@therichscript.local";

    [JsonIgnore]
    public string Initials
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FirstName))
            {
                var f = FirstName.Trim()[0].ToString().ToUpper();
                var l = !string.IsNullOrWhiteSpace(LastName) ? LastName.Trim()[0].ToString().ToUpper() : string.Empty;
                return $"{f}{l}";
            }
            if (!string.IsNullOrWhiteSpace(Email))
            {
                return Email.Trim()[0].ToString().ToUpper();
            }
            return "U";
        }
    }
}

public class AuthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = "IN";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";
}

public class UpdateProfileRequest
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public class UpdatePasswordRequest
{
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}
