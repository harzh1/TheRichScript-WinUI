using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("incomeSourceId")]
    public string? IncomeSourceId { get; set; }

    [JsonPropertyName("incomeSource")]
    public string? IncomeSourceName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

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
    public string Type { get; set; } = "EXPENSE";
    public double Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? IncomeSourceId { get; set; }
    public string? IncomeSourceName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string? ReceiptFilePath { get; set; }
}

public class UpdateTransactionRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string Type { get; set; } = "EXPENSE";
    public double Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? IncomeSourceId { get; set; }
    public string? IncomeSourceName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string? ReceiptFilePath { get; set; }
}
