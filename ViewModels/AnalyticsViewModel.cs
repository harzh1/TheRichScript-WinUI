using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private AnalyticsSummary? analyticsData;

    [ObservableProperty]
    private ObservableCollection<CategorySpendingSummary> categoryBreakdown = new();

    [ObservableProperty]
    private ObservableCollection<MonthlyTrendItem> monthlyTrends = new();

    [ObservableProperty]
    private ObservableCollection<DailySpendingItem> dailySpending = new();

    [ObservableProperty]
    private DateTimeOffset selectedMonth = DateTimeOffset.Now;

    [ObservableProperty]
    private int pickerYear = DateTime.Now.Year;

    [ObservableProperty]
    private string formattedSelectedMonth = DateTime.Now.ToString("MMMM yyyy");

    [ObservableProperty]
    private double savingsRate;

    [ObservableProperty]
    private string formattedSavingsRate = "0.0%";

    [ObservableProperty]
    private string currencySymbol = "₹";

    [ObservableProperty]
    private double maxTrendAmount;

    [ObservableProperty]
    private double midTrendAmount;

    [ObservableProperty]
    private bool hasMonthlyTrends;

    public AnalyticsViewModel(
        IAnalyticsService analyticsService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService)
    {
        _analyticsService = analyticsService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;

        CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _settingsService.CurrencyChanged += _ => CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _dataCacheService.DataUpdated += OnDataUpdated;
    }

    private void OnDataUpdated(DataCacheType types)
    {
        if (types.HasFlag(DataCacheType.Analytics) && IsCurrentMonthSelected())
        {
            var cached = _dataCacheService.CachedAnalytics;
            if (cached != null)
            {
                ApplyAnalyticsSummary(cached);
            }
        }
    }

    private bool IsCurrentMonthSelected()
    {
        var now = DateTime.Now;
        return SelectedMonth.Year == now.Year && SelectedMonth.Month == now.Month;
    }

    partial void OnSelectedMonthChanged(DateTimeOffset value)
    {
        FormattedSelectedMonth = value.ToString("MMMM yyyy");
        PickerYear = value.Year;
        _ = LoadAnalyticsDataAsync();
    }

    [RelayCommand]
    public void PreviousYear()
    {
        PickerYear--;
    }

    [RelayCommand]
    public void NextYear()
    {
        PickerYear++;
    }

    [RelayCommand]
    public void SetMonth(string monthNumStr)
    {
        if (int.TryParse(monthNumStr, out int monthNum) && monthNum >= 1 && monthNum <= 12)
        {
            SelectedMonth = new DateTimeOffset(new DateTime(PickerYear, monthNum, 1));
            FormattedSelectedMonth = SelectedMonth.ToString("MMMM yyyy");
        }
    }

    [RelayCommand]
    public void SetCurrentMonth()
    {
        PickerYear = DateTime.Now.Year;
        SelectedMonth = DateTimeOffset.Now;
        FormattedSelectedMonth = SelectedMonth.ToString("MMMM yyyy");
    }

    [RelayCommand]
    public async Task LoadAnalyticsDataAsync()
    {
        // If current month and data is in cache, display instantly
        if (IsCurrentMonthSelected() && _dataCacheService.CachedAnalytics != null)
        {
            ApplyAnalyticsSummary(_dataCacheService.CachedAnalytics);
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Analytics);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var monthQuery = SelectedMonth.ToString("yyyy-MM");
            var response = await _analyticsService.GetAnalyticsSummaryAsync(monthQuery);

            if (response.Success && response.Data != null)
            {
                ApplyAnalyticsSummary(response.Data);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading analytics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyAnalyticsSummary(AnalyticsSummary data)
    {
        AnalyticsData = data;

        if (AnalyticsData.MonthIncome > 0)
        {
            SavingsRate = Math.Max(0, (AnalyticsData.MonthNetSavings / AnalyticsData.MonthIncome) * 100.0);
            FormattedSavingsRate = $"{SavingsRate:0.0}%";
        }
        else
        {
            SavingsRate = 0;
            FormattedSavingsRate = "0.0%";
        }

        // Process Category Spending Breakdown
        CategoryBreakdown.Clear();
        if (AnalyticsData.ExpensesByCategory != null)
        {
            double totalMonthly = 0;
            foreach (var item in AnalyticsData.ExpensesByCategory.Values)
            {
                totalMonthly += item.Monthly;
            }

            foreach (var (catName, catData) in AnalyticsData.ExpensesByCategory.OrderByDescending(kv => kv.Value.Monthly))
            {
                if (catData.Monthly > 0 || catData.AllTime > 0)
                {
                    CategoryBreakdown.Add(new CategorySpendingSummary
                    {
                        CategoryName = catName,
                        Amount = catData.Monthly,
                        MonthlyLimit = catData.MonthlyLimit,
                        PercentageOfTotal = totalMonthly > 0 ? (catData.Monthly / totalMonthly) * 100.0 : 0
                    });
                }
            }
        }

        // Process Monthly Trends for Visual Chart
        MonthlyTrends.Clear();
        if (AnalyticsData.MonthlyTrends != null && AnalyticsData.MonthlyTrends.Count > 0)
        {
            HasMonthlyTrends = true;
            double maxVal = 1000;
            foreach (var t in AnalyticsData.MonthlyTrends)
            {
                if (t.Income > maxVal) maxVal = t.Income;
                if (t.Expense > maxVal) maxVal = t.Expense;
            }
            MaxTrendAmount = maxVal;
            MidTrendAmount = maxVal / 2.0;

            const double maxBarHeight = 140.0;

            foreach (var trend in AnalyticsData.MonthlyTrends)
            {
                trend.IncomeBarHeight = Math.Max(4, (trend.Income / maxVal) * maxBarHeight);
                trend.ExpenseBarHeight = Math.Max(4, (trend.Expense / maxVal) * maxBarHeight);
                MonthlyTrends.Add(trend);
            }
        }
        else
        {
            HasMonthlyTrends = false;
        }

        // Process Daily Spending for Trend Graph
        DailySpending.Clear();
        if (AnalyticsData.MonthDailySpending != null && AnalyticsData.MonthDailySpending.Count > 0)
        {
            foreach (var day in AnalyticsData.MonthDailySpending)
            {
                DailySpending.Add(day);
            }
        }
    }
}
