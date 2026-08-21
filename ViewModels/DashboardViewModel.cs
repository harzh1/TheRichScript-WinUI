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
        IBusyIndicatorService busyIndicatorService)
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

        // 2. Available Assets
        if (_dataCacheService.CachedAssets != null)
        {
            AvailableAssets.Clear();
            foreach (var a in _dataCacheService.CachedAssets)
            {
                AvailableAssets.Add(a);
            }
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

        // 5. Quick Add Templates (MANUAL plans)
        if (_dataCacheService.CachedPlans != null)
        {
            QuickAddTemplates.Clear();
            foreach (var plan in _dataCacheService.CachedPlans.Where(p => string.Equals(p.Type, "MANUAL", StringComparison.OrdinalIgnoreCase)))
            {
                QuickAddTemplates.Add(plan);
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
                    Category = template.Category,
                    AssetId = template.AssetId,
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
