using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class GoalsViewModel : ObservableObject
{
    private readonly IGoalService _goalService;
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;
    private readonly IFxRateService _fxRateService;

    private List<Goal> _rawGoals = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Goal> goals = new();

    [ObservableProperty]
    private ObservableCollection<Goal> filteredGoals = new();

    [ObservableProperty]
    private double totalTargetAmount;

    [ObservableProperty]
    private double totalSavedAmount;

    [ObservableProperty]
    private double remainingTargetAmount;

    [ObservableProperty]
    private double overallProgressPercentage;

    [ObservableProperty]
    private int completedGoalsCount;

    [ObservableProperty]
    private int activeGoalsCount;

    [ObservableProperty]
    private int totalGoalsCount;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedStatusFilter = "All Goals";

    [ObservableProperty]
    private ObservableCollection<string> statusFilters = new() { "All Goals", "In Progress", "Completed", "Pinned" };

    [ObservableProperty]
    private string currencySymbol = "₹";

    public GoalsViewModel(
        IGoalService goalService,
        IAssetService assetService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService,
        IFxRateService fxRateService)
    {
        _goalService = goalService;
        _assetService = assetService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;
        _busyIndicatorService = busyIndicatorService;
        _fxRateService = fxRateService;

        CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _settingsService.CurrencyChanged += _ =>
        {
            CurrencySymbol = _settingsService.Settings.CurrencySymbol;
            ApplyCachedData();
        };
        _dataCacheService.DataUpdated += OnDataUpdated;
    }

    private void OnDataUpdated(DataCacheType types)
    {
        if (types.HasFlag(DataCacheType.Goals))
        {
            ApplyCachedData();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = "All Goals";
    }

    private void ApplyCachedData()
    {
        var cached = _dataCacheService.CachedGoals;
        if (cached != null)
        {
            _rawGoals = new List<Goal>(cached);
            Goals.Clear();

            var baseCurrency = _settingsService.Settings.PreferredCurrency ?? "INR";
            double targetSum = 0;
            double savedSum = 0;
            int completedCount = 0;

            foreach (var g in _rawGoals)
            {
                var goalCurr = string.IsNullOrWhiteSpace(g.Currency) ? baseCurrency : g.Currency;
                var convertedTarget = _fxRateService.ConvertAmount(g.TargetAmount, goalCurr, baseCurrency);
                var convertedSaved = _fxRateService.ConvertAmount(g.CurrentAmount, goalCurr, baseCurrency);

                g.BaseCurrency = baseCurrency;
                g.BaseTargetAmount = convertedTarget;
                g.BaseCurrentAmount = convertedSaved;

                Goals.Add(g);
                targetSum += convertedTarget;
                savedSum += convertedSaved;
                if (g.IsCompleted) completedCount++;
            }

            TotalTargetAmount = targetSum;
            TotalSavedAmount = savedSum;
            RemainingTargetAmount = Math.Max(0, targetSum - savedSum);
            OverallProgressPercentage = targetSum > 0 ? Math.Min(100.0, (savedSum / targetSum) * 100.0) : 0;
            CompletedGoalsCount = completedCount;
            ActiveGoalsCount = _rawGoals.Count - completedCount;
            TotalGoalsCount = _rawGoals.Count;

            ApplyFilters();

            // Asynchronously preload live rates if foreign currencies are present
            var foreignCurrencies = _rawGoals
                .Select(g => g.Currency)
                .Where(c => !string.IsNullOrWhiteSpace(c) && !string.Equals(c, baseCurrency, StringComparison.OrdinalIgnoreCase))
                .Select(c => c!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (foreignCurrencies.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    await _fxRateService.PreloadRatesAsync(foreignCurrencies, baseCurrency);
                    App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
                    {
                        ApplyCachedData();
                    });
                });
            }
        }
    }

    private void ApplyFilters()
    {
        var query = _rawGoals.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(g => g.Name != null && g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatusFilter == "In Progress")
        {
            query = query.Where(g => !g.IsCompleted);
        }
        else if (SelectedStatusFilter == "Completed")
        {
            query = query.Where(g => g.IsCompleted);
        }
        else if (SelectedStatusFilter == "Pinned")
        {
            query = query.Where(g => g.IsPinned);
        }

        query = query.OrderByDescending(g => g.IsPinned).ThenByDescending(g => g.CreatedAt);

        FilteredGoals.Clear();
        foreach (var g in query)
        {
            FilteredGoals.Add(g);
        }
    }

    [RelayCommand]
    public async Task LoadGoalsAsync()
    {
        if (_dataCacheService.CachedGoals != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Goals);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await _dataCacheService.GetGoalsAsync();
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading goals");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TogglePinAsync(Goal goal)
    {
        if (goal == null) return;

        var previousPinned = goal.IsPinned;
        var newPinned = !previousPinned;

        // 1. Optimistic UI update
        goal.IsPinned = newPinned;
        ApplyFilters();

        // 2. Build full update request so backend validation schemas (name, targetAmount, currency, expectedDate) pass
        var request = new UpdateGoalRequest
        {
            Name = goal.Name,
            TargetAmount = goal.TargetAmount > 0 ? goal.TargetAmount : 1.0,
            Currency = !string.IsNullOrWhiteSpace(goal.Currency) ? goal.Currency.ToUpperInvariant() : "INR",
            ExpectedDate = goal.ExpectedDate == default ? DateTime.Now.AddMonths(6) : goal.ExpectedDate,
            RecurringAmount = goal.RecurringAmount,
            RecurringFrequency = goal.RecurringFrequency,
            SourceAssetId = goal.SourceAssetId,
            TargetAssetId = goal.TargetAssetId,
            IsPinned = newPinned,
            ImageUrls = goal.ImageUrls != null ? new List<string>(goal.ImageUrls) : new List<string>()
        };

        try
        {
            var response = await _goalService.UpdateGoalAsync(goal.Id, request);
            if (response.Success)
            {
                if (response.Data != null)
                {
                    goal.IsPinned = response.Data.IsPinned;
                }
                _dataCacheService.Invalidate(DataCacheType.Goals);
                ApplyFilters();
            }
            else
            {
                // Rollback on failure
                goal.IsPinned = previousPinned;
                ApplyFilters();
                await _dialogService.ShowErrorAsync(response.Error ?? "Failed to update goal pin status.", "Pin Goal");
            }
        }
        catch (Exception ex)
        {
            // Rollback on error
            goal.IsPinned = previousPinned;
            ApplyFilters();
            await _dialogService.ShowErrorAsync(ex.Message, "Pin Goal");
        }
    }

    [RelayCommand]
    private async Task DeleteGoalAsync(Goal goal)
    {
        if (goal == null) return;

        var assets = await _dataCacheService.GetAssetsAsync() ?? new List<Asset>();

        var dialog = new DeleteGoalDialog(goal, assets);
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result != ContentDialogResult.Primary) return;

        string? refundAssetId = goal.CurrentAmount > 0 ? dialog.SelectedRefundAssetId : null;

        var busyText = goal.CurrentAmount > 0
            ? $"Deleting goal '{goal.Name}' & refunding funds..."
            : $"Deleting goal '{goal.Name}'...";

        using (_busyIndicatorService.BeginBusy(busyText))
        {
            IsLoading = true;
            try
            {
                var response = await _goalService.DeleteGoalAsync(goal.Id, refundAssetId);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Goals | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                    await LoadGoalsAsync();

                    var successMsg = goal.CurrentAmount > 0
                        ? $"Goal '{goal.Name}' deleted and {CurrencySymbol}{goal.CurrentAmount:N2} refunded successfully."
                        : $"Goal '{goal.Name}' deleted.";
                    await _dialogService.ShowMessageAsync("Success", successMsg);
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete goal.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
