using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ITransactionService _transactionService;
    private readonly IGoalService _goalService;
    private readonly IAssetService _assetService;
    private readonly IPlanService _planService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;
    private readonly IFxRateService _fxRateService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private AnalyticsSummary? analytics;

    [ObservableProperty]
    private ObservableCollection<Transaction> recentTransactions = new();

    [ObservableProperty]
    private ObservableCollection<Goal> pinnedGoals = new();

    [ObservableProperty]
    private ObservableCollection<CategorySpendingSummary> topExpenseCategories = new();

    [ObservableProperty]
    private ObservableCollection<PlannedExpense> quickAddTemplates = new();

    [ObservableProperty]
    private ObservableCollection<Asset> availableAssets = new();

    [ObservableProperty]
    private string currencySymbol = "₹";

    public DashboardViewModel(
        IAnalyticsService analyticsService,
        ITransactionService transactionService,
        IGoalService goalService,
        IAssetService assetService,
        IPlanService planService,
        IDialogService dialogService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService,
        IFxRateService fxRateService)
    {
        _analyticsService = analyticsService;
        _transactionService = transactionService;
        _goalService = goalService;
        _assetService = assetService;
        _planService = planService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;
        _busyIndicatorService = busyIndicatorService;
        _fxRateService = fxRateService;

        CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _settingsService.CurrencyChanged += _ => CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _dataCacheService.DataUpdated += OnDataUpdated;
    }

    private void OnDataUpdated(DataCacheType types)
    {
        if (types.HasFlag(DataCacheType.Analytics) ||
            types.HasFlag(DataCacheType.Transactions) ||
            types.HasFlag(DataCacheType.Goals) ||
            types.HasFlag(DataCacheType.Plans) ||
            types.HasFlag(DataCacheType.Assets))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        // 1. Analytics
        if (_dataCacheService.CachedAnalytics != null)
        {
            Analytics = _dataCacheService.CachedAnalytics;
            if (!string.IsNullOrWhiteSpace(Analytics.Currency))
            {
                CurrencySymbol = CountryCurrencyData.GetCurrencySymbol(Analytics.Currency);
            }

            TopExpenseCategories.Clear();
            if (Analytics.ExpensesByCategory != null)
            {
                double totalMonthly = 0;
                foreach (var item in Analytics.ExpensesByCategory.Values)
                {
                    totalMonthly += item.Monthly;
                }

                foreach (var (catName, catData) in Analytics.ExpensesByCategory)
                {
                    if (catData.Monthly > 0)
                    {
                        TopExpenseCategories.Add(new CategorySpendingSummary
                        {
                            CategoryName = catName,
                            Amount = catData.Monthly,
                            MonthlyLimit = catData.MonthlyLimit,
                            PercentageOfTotal = totalMonthly > 0 ? (catData.Monthly / totalMonthly) * 100.0 : 0
                        });
                    }
                }
            }
        }
        else if (Analytics == null && _dataCacheService.CachedAssets != null)
        {
            var baseCurrency = _settingsService.Settings.PreferredCurrency ?? "INR";
            double total = 0;
            foreach (var a in _dataCacheService.CachedAssets)
            {
                var aCurr = string.IsNullOrWhiteSpace(a.Currency) ? baseCurrency : a.Currency;
                total += _fxRateService.ConvertAmount(a.Value, aCurr, baseCurrency);
            }
            Analytics = new AnalyticsSummary
            {
                Currency = baseCurrency,
                TotalPortfolioValue = total
            };
        }

        // 2. Available Assets - sync in-place without destructive Clear()
        if (_dataCacheService.CachedAssets != null)
        {
            SyncAssetsCollection(AvailableAssets, _dataCacheService.CachedAssets);
        }

        // 3. Recent Transactions
        if (_dataCacheService.CachedTransactions != null)
        {
            RecentTransactions.Clear();
            foreach (var tx in _dataCacheService.CachedTransactions.Take(6))
            {
                RecentTransactions.Add(tx);
            }
        }

        // 4. Pinned Goals
        if (_dataCacheService.CachedGoals != null)
        {
            PinnedGoals.Clear();
            foreach (var goal in _dataCacheService.CachedGoals)
            {
                if (goal.IsPinned || PinnedGoals.Count < 3)
                {
                    PinnedGoals.Add(goal);
                }
            }
        }

        // 5. Quick Add Templates (MANUAL plans) - sync in-place without destructive Clear()
        if (_dataCacheService.CachedPlans != null)
        {
            SyncPlansCollection(QuickAddTemplates, _dataCacheService.CachedPlans, AvailableAssets);
        }
    }

    private static void SyncAssetsCollection(ObservableCollection<Asset> target, System.Collections.Generic.IList<Asset> source)
    {
        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!source.Any(s => s.Id == target[i].Id))
            {
                target.RemoveAt(i);
            }
        }

        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i];
            var existingIndex = -1;
            for (int j = 0; j < target.Count; j++)
            {
                if (target[j].Id == src.Id) { existingIndex = j; break; }
            }

            if (existingIndex < 0)
            {
                target.Insert(Math.Min(i, target.Count), src);
            }
            else if (target[existingIndex].Value != src.Value || target[existingIndex].Name != src.Name)
            {
                target[existingIndex] = src;
            }
        }
    }

    private static void SyncPlansCollection(ObservableCollection<PlannedExpense> target, System.Collections.Generic.IList<PlannedExpense> source, ObservableCollection<Asset> availableAssets)
    {
        var defaultAssetId = availableAssets.FirstOrDefault()?.Id;
        var manualPlans = source.Where(p => string.Equals(p.Type, "MANUAL", StringComparison.OrdinalIgnoreCase)).ToList();

        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!manualPlans.Any(s => s.Id == target[i].Id))
            {
                target.RemoveAt(i);
            }
        }

        for (int i = 0; i < manualPlans.Count; i++)
        {
            var src = manualPlans[i];
            var existing = target.FirstOrDefault(t => t.Id == src.Id);

            if (existing == null)
            {
                if (string.IsNullOrEmpty(src.AssetId) && !string.IsNullOrEmpty(defaultAssetId))
                {
                    src.AssetId = defaultAssetId;
                }
                target.Insert(Math.Min(i, target.Count), src);
            }
            else
            {
                existing.Name = src.Name;
                existing.Category = src.Category;
                existing.CategoryId = src.CategoryId;
                existing.Type = src.Type;
                existing.TxType = src.TxType;

                if (string.IsNullOrEmpty(existing.AssetId))
                {
                    existing.AssetId = !string.IsNullOrEmpty(src.AssetId) ? src.AssetId : defaultAssetId;
                }
            }
        }
    }

    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        // If data is already in cache, populate immediately with ZERO loading delay
        if (_dataCacheService.CachedAnalytics != null || _dataCacheService.CachedTransactions != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Trigger silent background revalidation
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Analytics | DataCacheType.Transactions | DataCacheType.Goals | DataCacheType.Plans | DataCacheType.Assets);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await _dataCacheService.WarmUpAllDataAsync();
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Failed to load dashboard");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LogQuickAddTransactionAsync(PlannedExpense template)
    {
        if (template == null) return;

        using (_busyIndicatorService.BeginBusy($"Logging '{template.Name}'..."))
        {
            try
            {
                var tx = new CreateTransactionRequest
                {
                    Description = template.Name,
                    Amount = template.Amount,
                    Type = template.TxType ?? "EXPENSE",
                    Category = template.Category?.ToUpperInvariant(),
                    AssetId = template.AssetId ?? string.Empty,
                    Date = DateTime.Now
                };

                var response = await _transactionService.CreateTransactionAsync(tx);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Transactions | DataCacheType.Assets | DataCacheType.Analytics);
                    await _dialogService.ShowMessageAsync("Logged ✓", $"Successfully logged '{template.Name}' for {CurrencySymbol}{template.Amount:N2}.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to log transaction.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
        }
    }

    [RelayCommand]
    public async Task DeleteQuickAddTemplateAsync(PlannedExpense template)
    {
        if (template == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Quick Add Template",
            $"Are you sure you want to delete the template '{template.Name}'?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy($"Deleting template '{template.Name}'..."))
        {
            try
            {
                var response = await _planService.DeletePlanAsync(template.Id);
                if (response.Success)
                {
                    QuickAddTemplates.Remove(template);
                    _dataCacheService.Invalidate(DataCacheType.Plans);
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete template.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
        }
    }

    [RelayCommand]
    private void NavigateToTransactions()
    {
        _navigationService.NavigateTo(typeof(Views.TransactionsPage));
    }

    [RelayCommand]
    private void NavigateToAssets()
    {
        _navigationService.NavigateTo(typeof(Views.AssetsPage));
    }

    [RelayCommand]
    private void NavigateToGoals()
    {
        _navigationService.NavigateTo(typeof(Views.GoalsPage));
    }

    [RelayCommand]
    private void NavigateToAnalytics()
    {
        _navigationService.NavigateTo(typeof(Views.AnalyticsPage));
    }
}
