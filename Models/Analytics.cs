using System;
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
}

public class DailySpendingItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("dateStr")]
    public string DateStr { get; set; } = string.Empty;

    [JsonPropertyName("expense")]
    public double Expense { get; set; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? CategoryExpenses { get; set; }

    public double GetCategoryExpense(string categoryName)
    {
        if (CategoryExpenses != null && CategoryExpenses.TryGetValue(categoryName, out var element))
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetDouble(out var val))
            {
                return val;
            }
        }
        return 0;
    }
}

public partial class DailySpendingCategoryFilter : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#F43F5E";
    public Microsoft.UI.Xaml.Media.SolidColorBrush ColorBrush { get; set; } = new(Microsoft.UI.Colors.Gray);
    public bool IsTotal { get; set; }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isSelected;
}

public class AnalyticsSummary
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

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
    public string ColorHex { get; set; } = "#6366F1";
    public Microsoft.UI.Xaml.Media.SolidColorBrush ColorBrush { get; set; } = new(Microsoft.UI.Colors.Gray);
    public double PercentageOfLimit => MonthlyLimit.HasValue && MonthlyLimit.Value > 0 ? (Amount / MonthlyLimit.Value) * 100.0 : 0;
    public bool IsOverLimit => MonthlyLimit.HasValue && Amount > MonthlyLimit.Value;

    [JsonIgnore]
    public string StatusText => IsOverLimit 
        ? "OVER LIMIT" 
        : (PercentageOfLimit >= 80 ? "NEAR LIMIT" : "ON TRACK");
}

public class GoalProgressItem
{
    public string Name { get; set; } = string.Empty;
    public double CurrentAmount { get; set; }
    public double TargetAmount { get; set; }
    public double ProgressPercentage { get; set; }
    public double RemainingAmount { get; set; }
}
