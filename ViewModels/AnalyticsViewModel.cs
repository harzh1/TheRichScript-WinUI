using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;

    // Curated high-contrast palette for categories & chart series
    private static readonly string[] CategoryHexPalette =
    [
        "#6366F1", // Indigo
        "#F43F5E", // Rose
        "#10B981", // Emerald Green
        "#F59E0B", // Amber
        "#06B6D4", // Cyan
        "#8B5CF6", // Purple
        "#3B82F6", // Blue
        "#F97316", // Orange
        "#14B8A6", // Teal
        "#EC4899", // Fuchsia
    ];

    // ── Observable State ──────────────────────────────────────────────

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private AnalyticsSummary? analyticsData;

    [ObservableProperty]
    private ObservableCollection<CategorySpendingSummary> categoryBreakdown = new();

    [ObservableProperty]
    private ObservableCollection<DailySpendingCategoryFilter> dailyCategoryFilters = new();

    [ObservableProperty]
    private string selectedSeriesSummary = "Total Expenses";

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
    private string formattedTotalExpense = "₹ 0.00";

    [ObservableProperty]
    private string dailySpendingSubtitle = string.Empty;

    [ObservableProperty]
    private string currencySymbol = "₹";

    [ObservableProperty]
    private bool hasDailySpending;

    [ObservableProperty]
    private bool hasCategoryData;

    [ObservableProperty]
    private bool hasGoalsData;

    // Smooth cubic easing for charts
    [ObservableProperty]
    private Func<float, float> chartEasing = EasingFunctions.CubicOut;

    // ── LiveCharts2 Series & Axes ─────────────────────────────────────

    [ObservableProperty]
    private ISeries[] dailySpendingSeries = [];

    [ObservableProperty]
    private LiveChartsCore.Kernel.Sketches.ICartesianAxis[] dailySpendingXAxes = [];

    [ObservableProperty]
    private LiveChartsCore.Kernel.Sketches.ICartesianAxis[] dailySpendingYAxes = [];

    [ObservableProperty]
    private ISeries[] categoryPieSeries = [];

    [ObservableProperty]
    private ObservableCollection<GoalProgressItem> goalsProgress = new();

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

    // ── Month Picker Commands ─────────────────────────────────────────

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

    // ── Category Filter Dropdown Commands ─────────────────────────────

    [RelayCommand]
    public void SelectAllCategoryFilters()
    {
        foreach (var filter in DailyCategoryFilters)
        {
            filter.IsSelected = true;
        }
        RebuildDailySpendingSeries();
    }

    [RelayCommand]
    public void ResetCategoryFilters()
    {
        foreach (var filter in DailyCategoryFilters)
        {
            filter.IsSelected = filter.IsTotal;
        }
        RebuildDailySpendingSeries();
    }

    // ── Data Loading ──────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAnalyticsDataAsync()
    {
        if (IsCurrentMonthSelected() && _dataCacheService.CachedAnalytics != null)
        {
            ApplyAnalyticsSummary(_dataCacheService.CachedAnalytics);
            IsLoading = false;
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

    // ── Apply Data to Charts & UI ─────────────────────────────────────

    private void ApplyAnalyticsSummary(AnalyticsSummary data)
    {
        AnalyticsData = data;
        if (!string.IsNullOrWhiteSpace(data.Currency))
        {
            CurrencySymbol = CountryCurrencyData.GetCurrencySymbol(data.Currency);
        }

        // Savings Rate & Total Expense
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

        FormattedTotalExpense = $"{CurrencySymbol} {AnalyticsData.MonthExpense:N2}";

        // Build charts & components
        BuildCategoryBreakdownAndDonut(data);
        BuildDailySpendingChart(data);
        LoadGoalsProgress();
    }

    // ── Category Breakdown & Donut Chart ──────────────────────────────

    private void BuildCategoryBreakdownAndDonut(AnalyticsSummary data)
    {
        CategoryBreakdown.Clear();

        if (data.ExpensesByCategory == null || data.ExpensesByCategory.Count == 0)
        {
            HasCategoryData = false;
            CategoryPieSeries = [];
            return;
        }

        var ordered = data.ExpensesByCategory
            .Where(kv => kv.Value.Monthly > 0)
            .OrderByDescending(kv => kv.Value.Monthly)
            .ToList();

        if (ordered.Count == 0)
        {
            HasCategoryData = false;
            CategoryPieSeries = [];
            return;
        }

        HasCategoryData = true;
        double totalMonthly = ordered.Sum(kv => kv.Value.Monthly);

        var seriesList = new List<ISeries>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var hex = CategoryHexPalette[i % CategoryHexPalette.Length];
            var skColor = SKColor.Parse(hex);
            var brush = HexToBrush(hex);
            var item = ordered[i];

            CategoryBreakdown.Add(new CategorySpendingSummary
            {
                CategoryName = item.Key,
                Amount = item.Value.Monthly,
                MonthlyLimit = item.Value.MonthlyLimit,
                PercentageOfTotal = totalMonthly > 0 ? (item.Value.Monthly / totalMonthly) * 100.0 : 0,
                ColorHex = hex,
                ColorBrush = brush,
            });

            seriesList.Add(new PieSeries<double>
            {
                Values = [item.Value.Monthly],
                Name = item.Key,
                Fill = new SolidColorPaint(skColor),
                InnerRadius = 65,
                HoverPushout = 4,
                DataLabelsPaint = null,
            });
        }

        CategoryPieSeries = seriesList.ToArray();
    }

    // ── Daily Spending Area Chart & Category Multi-Series ─────────────

    private void BuildDailySpendingChart(AnalyticsSummary data)
    {
        if (data.MonthDailySpending == null || data.MonthDailySpending.Count == 0)
        {
            HasDailySpending = false;
            DailySpendingSeries = [];
            DailySpendingSubtitle = string.Empty;
            DailyCategoryFilters.Clear();
            return;
        }

        HasDailySpending = true;

        var labels = data.MonthDailySpending.Select(d => d.Label).ToArray();

        // Calculate peak day and daily average for subtitle
        double maxExpense = 0;
        string peakDay = "1";
        double totalExpense = 0;
        int activeDays = 0;

        for (int i = 0; i < data.MonthDailySpending.Count; i++)
        {
            var item = data.MonthDailySpending[i];
            totalExpense += item.Expense;
            if (item.Expense > maxExpense)
            {
                maxExpense = item.Expense;
                peakDay = item.Label;
            }
            if (item.Expense > 0) activeDays++;
        }

        double dailyAvg = data.MonthDailySpending.Count > 0 ? totalExpense / data.MonthDailySpending.Count : 0;
        DailySpendingSubtitle = $"Daily Avg: {CurrencySymbol}{dailyAvg:N0} • Peak: Day {peakDay} ({CurrencySymbol}{maxExpense:N0})";

        // Setup Axes
        DailySpendingXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                ForceStepToMin = true,
                MinStep = Math.Max(1, labels.Length / 8),
                SeparatorsPaint = null,
                TicksPaint = null,
                SubticksPaint = null,
            }
        ];

        DailySpendingYAxes =
        [
            new Axis
            {
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                Labeler = val => FormatCurrencyShort(val),
                MinLimit = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 14), 1f),
                TicksPaint = null,
                SubticksPaint = null,
            }
        ];

        // Build Category Filter items
        DailyCategoryFilters.Clear();

        var totalFilter = new DailySpendingCategoryFilter
        {
            Name = "Total Expenses",
            ColorHex = "#F43F5E",
            ColorBrush = HexToBrush("#F43F5E"),
            IsTotal = true,
            IsSelected = true,
        };
        totalFilter.PropertyChanged += OnFilterPropertyChanged;
        DailyCategoryFilters.Add(totalFilter);

        // Add each category with its corresponding palette color
        if (data.ExpensesByCategory != null)
        {
            int colorIdx = 0;
            foreach (var (catName, catData) in data.ExpensesByCategory.OrderByDescending(kv => kv.Value.Monthly))
            {
                var hex = CategoryHexPalette[colorIdx % CategoryHexPalette.Length];
                colorIdx++;

                var catFilter = new DailySpendingCategoryFilter
                {
                    Name = catName,
                    ColorHex = hex,
                    ColorBrush = HexToBrush(hex),
                    IsTotal = false,
                    IsSelected = false,
                };
                catFilter.PropertyChanged += OnFilterPropertyChanged;
                DailyCategoryFilters.Add(catFilter);
            }
        }

        RebuildDailySpendingSeries();
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DailySpendingCategoryFilter.IsSelected))
        {
            RebuildDailySpendingSeries();
        }
    }

    public void RebuildDailySpendingSeries()
    {
        if (AnalyticsData?.MonthDailySpending == null || AnalyticsData.MonthDailySpending.Count == 0)
        {
            DailySpendingSeries = [];
            return;
        }

        var selectedFilters = DailyCategoryFilters.Where(f => f.IsSelected).ToList();

        if (selectedFilters.Count == 0)
        {
            SelectedSeriesSummary = "No Series Selected";
            DailySpendingSeries = [];
            return;
        }

        if (selectedFilters.Count == 1)
        {
            SelectedSeriesSummary = selectedFilters[0].Name;
        }
        else
        {
            SelectedSeriesSummary = $"{selectedFilters.Count} Series Active";
        }

        var seriesList = new List<ISeries>();

        foreach (var filter in selectedFilters)
        {
            var skColor = SKColor.Parse(filter.ColorHex);
            double[] values;

            if (filter.IsTotal)
            {
                values = AnalyticsData.MonthDailySpending.Select(d => d.Expense).ToArray();
            }
            else
            {
                values = AnalyticsData.MonthDailySpending.Select(d => d.GetCategoryExpense(filter.Name)).ToArray();
            }

            seriesList.Add(new LineSeries<double>
            {
                Values = values,
                Name = filter.Name,
                Fill = new LinearGradientPaint(
                    [skColor.WithAlpha(35), skColor.WithAlpha(0)],
                    new SKPoint(0.5f, 0),
                    new SKPoint(0.5f, 1)),
                Stroke = new SolidColorPaint(skColor, 2.3f),
                GeometrySize = 4,
                GeometryFill = new SolidColorPaint(skColor),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#202020"), 1.5f),
                LineSmoothness = 0.45,
            });
        }

        DailySpendingSeries = seriesList.ToArray();
    }

    // ── Goals Progress ────────────────────────────────────────────────

    private void LoadGoalsProgress()
    {
        GoalsProgress.Clear();
        var goals = _dataCacheService.CachedGoals;
        if (goals == null || goals.Count == 0)
        {
            HasGoalsData = false;
            return;
        }

        foreach (var goal in goals.Where(g => !g.IsCompleted).OrderByDescending(g => g.ProgressPercentage))
        {
            GoalsProgress.Add(new GoalProgressItem
            {
                Name = goal.Name,
                CurrentAmount = goal.CurrentAmount,
                TargetAmount = goal.TargetAmount,
                ProgressPercentage = goal.ProgressPercentage,
                RemainingAmount = goal.RemainingAmount,
            });
        }

        HasGoalsData = GoalsProgress.Count > 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private string FormatCurrencyShort(double value)
    {
        if (value >= 100000) return $"{CurrencySymbol}{value / 100000:0.#}L";
        if (value >= 1000) return $"{CurrencySymbol}{value / 1000:0.#}K";
        return $"{CurrencySymbol}{value:0}";
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush HexToBrush(string hex)
    {
        hex = hex.Replace("#", "");
        byte a = 255;
        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
    }
}
