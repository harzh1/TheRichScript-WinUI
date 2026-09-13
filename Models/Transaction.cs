using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Models;

public class Transaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("toAssetId")]
    public string? ToAssetId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "EXPENSE"; // INCOME, EXPENSE, TRANSFER

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Amount { get; set; }

    [JsonPropertyName("exchangeRate")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? ExchangeRate { get; set; }

    [JsonPropertyName("targetAmount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? TargetAmount { get; set; }

    [JsonPropertyName("baseAmount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? BaseAmount { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("incomeSourceId")]
    public string? IncomeSourceId { get; set; }

    [JsonPropertyName("incomeSource")]
    public string? IncomeSourceName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonIgnore]
    public bool HasCategory => !string.IsNullOrWhiteSpace(Category) && Category.Trim() != "—";

    [JsonIgnore]
    public string DisplayCategory => HasCategory ? Category!.Trim() : string.Empty;

    [JsonIgnore]
    public string DisplayDescription => !string.IsNullOrWhiteSpace(Description) ? Description : "—";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("receiptImageUrl")]
    public string? ReceiptImageUrl { get; set; }

    [JsonPropertyName("linkedDebtId")]
    public string? LinkedDebtId { get; set; }

    [JsonPropertyName("linkedDebtAction")]
    public string? LinkedDebtAction { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public bool IsIncome => string.Equals(Type, "INCOME", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsExpense => string.Equals(Type, "EXPENSE", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsTransfer => string.Equals(Type, "TRANSFER", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string FormattedAmountPrefix => IsIncome ? "+ " : (IsExpense ? "- " : "");

    [JsonIgnore]
    public string CurrencySymbol => CountryCurrencyData.GetCurrencySymbol(Currency);

    [JsonIgnore]
    public string FormattedAmount => $"{FormattedAmountPrefix}{CurrencySymbol}{Amount:N2}";

    [JsonIgnore]
    public bool HasConvertedSubtitle => BaseAmount.HasValue && !string.IsNullOrWhiteSpace(Currency) && Math.Abs(BaseAmount.Value - Amount) > 0.0001;

    [JsonIgnore]
    public string ConvertedSubtitle
    {
        get
        {
            if (BaseAmount.HasValue)
            {
                var baseSymbol = "₹";
                try
                {
                    var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
                    if (settings?.Settings?.CurrencySymbol != null)
                    {
                        baseSymbol = settings.Settings.CurrencySymbol;
                    }
                }
                catch { }
                return $"Charged as {baseSymbol}{BaseAmount.Value:N2}";
            }
            return string.Empty;
        }
    }

    [JsonIgnore]
    public string AccountDisplay { get; set; } = string.Empty;
}

public class TransactionMeta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

public class TransactionListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<Transaction> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public TransactionMeta? Meta { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class CreateTransactionRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string? ToAssetId { get; set; }
    public string Type { get; set; } = "EXPENSE";
    public double Amount { get; set; }
    public string? Currency { get; set; }
    public double? ExchangeRate { get; set; }
    public double? TargetAmount { get; set; }
    public string? Category { get; set; }
    public string? IncomeSourceId { get; set; }
    public string? IncomeSourceName { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string? ReceiptFilePath { get; set; }
}

public class UpdateTransactionRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string? ToAssetId { get; set; }
    public string Type { get; set; } = "EXPENSE";
    public double Amount { get; set; }
    public string? Currency { get; set; }
    public double? ExchangeRate { get; set; }
    public double? TargetAmount { get; set; }
    public string? Category { get; set; }
    public string? IncomeSourceId { get; set; }
    public string? IncomeSourceName { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string? ReceiptFilePath { get; set; }
}
