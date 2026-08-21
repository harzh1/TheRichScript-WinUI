using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheRichScript_WinUI.Models;

public class CategoryExpenseItem
{
    [JsonPropertyName("allTime")]
    public double AllTime { get; set; }

    [JsonPropertyName("monthly")]
    public double Monthly { get; set; }

    [JsonPropertyName("yearly")]
    public double Yearly { get; set; }

    [JsonPropertyName("monthlyLimit")]
    public double? MonthlyLimit { get; set; }

    [JsonPropertyName("yearlyLimit")]
    public double? YearlyLimit { get; set; }
}

public class MonthlyTrendItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("income")]
    public double Income { get; set; }

    [JsonPropertyName("expense")]
    public double Expense { get; set; }

    [JsonIgnore]
    public double NetSavings => Income - Expense;

    [JsonIgnore]
    public double IncomeBarHeight { get; set; } = 4;

    [JsonIgnore]
    public double ExpenseBarHeight { get; set; } = 4;

    [JsonIgnore]
    public string FormattedIncome { get; set; } = string.Empty;

    [JsonIgnore]
    public string FormattedExpense { get; set; } = string.Empty;

    [JsonIgnore]
    public string FormattedNetSavings { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsPositiveNet => NetSavings >= 0;

    [JsonIgnore]
    public string ChartTooltip => $"{Label}\nIncome: {FormattedIncome}\nExpense: {FormattedExpense}\nNet: {FormattedNetSavings}";
}

public class DailySpendingItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("dateStr")]
    public string DateStr { get; set; } = string.Empty;

    [JsonPropertyName("expense")]
    public double Expense { get; set; }
}

public class AnalyticsSummary
{
    [JsonPropertyName("totalPortfolioValue")]
    public double TotalPortfolioValue { get; set; }

    [JsonPropertyName("totalIncome")]
    public double TotalIncome { get; set; }

    [JsonPropertyName("totalExpense")]
    public double TotalExpense { get; set; }

    [JsonPropertyName("monthIncome")]
    public double MonthIncome { get; set; }

    [JsonPropertyName("monthExpense")]
    public double MonthExpense { get; set; }

    [JsonPropertyName("balance")]
    public double Balance { get; set; }

    [JsonPropertyName("expensesByCategory")]
    public Dictionary<string, CategoryExpenseItem> ExpensesByCategory { get; set; } = new();

    [JsonPropertyName("monthlyTrends")]
    public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new();

    [JsonPropertyName("monthDailySpending")]
    public List<DailySpendingItem> MonthDailySpending { get; set; } = new();

    [JsonIgnore]
    public double MonthNetSavings => MonthIncome - MonthExpense;
}

public class CategorySpendingSummary
{
    public string CategoryName { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double? MonthlyLimit { get; set; }
    public double PercentageOfTotal { get; set; }
    public double PercentageOfLimit => MonthlyLimit.HasValue && MonthlyLimit.Value > 0 ? (Amount / MonthlyLimit.Value) * 100.0 : 0;
    public bool IsOverLimit => MonthlyLimit.HasValue && Amount > MonthlyLimit.Value;

    [JsonIgnore]
    public double RemainingBudget => MonthlyLimit.HasValue ? Math.Max(0, MonthlyLimit.Value - Amount) : 0;

    [JsonIgnore]
    public string StatusText => IsOverLimit 
        ? "OVER LIMIT" 
        : (PercentageOfLimit >= 80 ? "NEAR LIMIT" : "ON TRACK");
}
