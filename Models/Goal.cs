using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Models;

public class Goal : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    private string _name = string.Empty;
    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    private double _targetAmount;
    [JsonPropertyName("targetAmount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double TargetAmount
    {
        get => _targetAmount;
        set
        {
            if (_targetAmount != value)
            {
                _targetAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(FormattedTargetAmount));
                OnPropertyChanged(nameof(FormattedProgress));
            }
        }
    }

    private double _currentAmount;
    [JsonPropertyName("currentAmount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double CurrentAmount
    {
        get => _currentAmount;
        set
        {
            if (_currentAmount != value)
            {
                _currentAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(FormattedCurrentAmount));
                OnPropertyChanged(nameof(FormattedProgress));
            }
        }
    }

    [JsonPropertyName("expectedDate")]
    public DateTime ExpectedDate { get; set; }

    private bool _isPinned;
    [JsonPropertyName("isPinned")]
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned != value)
            {
                _isPinned = value;
                OnPropertyChanged();
            }
        }
    }

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
    public string CurrencySymbol => CountryCurrencyData.GetCurrencySymbol(Currency);

    [JsonIgnore]
    public string FormattedCurrentAmount => $"{CurrencySymbol}{CurrentAmount:N2}";

    [JsonIgnore]
    public string FormattedTargetAmount => $"{CurrencySymbol}{TargetAmount:N2}";

    [JsonIgnore]
    public string FormattedProgress => $"{CurrencySymbol}{CurrentAmount:N0} / {CurrencySymbol}{TargetAmount:N0} ({ProgressPercentage:F1}%)";

    [JsonIgnore]
    public double BaseTargetAmount { get; set; }

    [JsonIgnore]
    public double BaseCurrentAmount { get; set; }

    [JsonIgnore]
    public string? BaseCurrency { get; set; }

    [JsonIgnore]
    public string FormattedBaseProgress => $"≈ {CountryCurrencyData.GetCurrencySymbol(BaseCurrency)}{BaseCurrentAmount:N0} / {CountryCurrencyData.GetCurrencySymbol(BaseCurrency)}{BaseTargetAmount:N0}";

    [JsonIgnore]
    public bool ShowBaseValue => !string.IsNullOrWhiteSpace(BaseCurrency) &&
                                 !string.IsNullOrWhiteSpace(Currency) &&
                                 !string.Equals(Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase);
}

public class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public double TargetAmount { get; set; }
    public double InitialDeposit { get; set; }
    public string? Currency { get; set; }
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

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

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

    [JsonPropertyName("imageUrls")]
    public List<string>? ImageUrls { get; set; }
}

public class AddGoalFundsRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("destinationAmount")]
    public double? DestinationAmount { get; set; }

    [JsonPropertyName("exchangeRate")]
    public double? ExchangeRate { get; set; }
}

public class DeleteGoalRequest
{
    [JsonPropertyName("refundAssetId")]
    public string? RefundAssetId { get; set; }
}
