using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Goal> goals = new();

    [ObservableProperty]
    private double totalTargetAmount;

    [ObservableProperty]
    private double totalSavedAmount;

    [ObservableProperty]
    private double overallProgressPercentage;

    [ObservableProperty]
    private string currencySymbol = "₹";

    public GoalsViewModel(
        IGoalService goalService,
        IAssetService assetService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _goalService = goalService;
        _assetService = assetService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;
        _busyIndicatorService = busyIndicatorService;

        CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _settingsService.CurrencyChanged += _ => CurrencySymbol = _settingsService.Settings.CurrencySymbol;
        _dataCacheService.DataUpdated += OnDataUpdated;
    }

    private void OnDataUpdated(DataCacheType types)
    {
        if (types.HasFlag(DataCacheType.Goals))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var cached = _dataCacheService.CachedGoals;
        if (cached != null)
        {
            Goals.Clear();
            double targetSum = 0;
            double savedSum = 0;

            foreach (var g in cached)
            {
                Goals.Add(g);
                targetSum += g.TargetAmount;
                savedSum += g.CurrentAmount;
            }

            TotalTargetAmount = targetSum;
            TotalSavedAmount = savedSum;
            OverallProgressPercentage = targetSum > 0 ? Math.Min(100.0, (savedSum / targetSum) * 100.0) : 0;
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

        var newPinned = !goal.IsPinned;
        var request = new UpdateGoalRequest { IsPinned = newPinned };

        try
        {
            var response = await _goalService.UpdateGoalAsync(goal.Id, request);
            if (response.Success)
            {
                goal.IsPinned = newPinned;
                _dataCacheService.Invalidate(DataCacheType.Goals);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteGoalAsync(Goal goal)
    {
        if (goal == null) return;

        if (goal.CurrentAmount > 0 && string.IsNullOrEmpty(goal.TargetAssetId))
        {
            // Goal has balance - ask user for refund asset
            var assetsResponse = await _dataCacheService.GetAssetsAsync();
            if (assetsResponse != null && assetsResponse.Count > 0)
            {
                var defaultRefundAsset = assetsResponse[0].Id;
                var confirmed = await _dialogService.ShowConfirmationAsync(
                    "Delete Goal & Refund Balance",
                    $"This goal has a saved balance of {CurrencySymbol}{goal.CurrentAmount:N2}. Deleting it will refund this amount back to your primary asset '{assetsResponse[0].Name}'. Proceed?",
                    "Refund & Delete",
                    "Cancel");

                if (!confirmed) return;

                using (_busyIndicatorService.BeginBusy($"Deleting goal '{goal.Name}' & refunding funds..."))
                {
                    IsLoading = true;
                    try
                    {
                        var response = await _goalService.DeleteGoalAsync(goal.Id, defaultRefundAsset);
                        if (response.Success)
                        {
                            _dataCacheService.Invalidate(DataCacheType.Goals | DataCacheType.Assets | DataCacheType.Analytics);
                            await _dialogService.ShowMessageAsync("Success", $"Goal '{goal.Name}' deleted and funds refunded.");
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
                return;
            }
        }

        var simpleConfirm = await _dialogService.ShowConfirmationAsync(
            "Delete Goal",
            $"Are you sure you want to delete goal '{goal.Name}'?",
            "Delete",
            "Cancel");

        if (!simpleConfirm) return;

        using (_busyIndicatorService.BeginBusy($"Deleting goal '{goal.Name}'..."))
        {
            IsLoading = true;
            try
            {
                var response = await _goalService.DeleteGoalAsync(goal.Id);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Goals);
                    await _dialogService.ShowMessageAsync("Success", $"Goal '{goal.Name}' deleted.");
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
