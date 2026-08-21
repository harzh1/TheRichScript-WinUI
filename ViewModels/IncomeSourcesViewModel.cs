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

public partial class IncomeSourcesViewModel : ObservableObject
{
    private readonly IIncomeSourceService _incomeSourceService;
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    private List<IncomeSource> _allSources = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<IncomeSource> incomeSources = new();

    [ObservableProperty]
    private List<Asset> availableAssets = new();

    // Summary Metrics
    [ObservableProperty]
    private double totalActiveMonthly;

    [ObservableProperty]
    private double totalPassiveMonthly;

    [ObservableProperty]
    private double totalExpectedMonthly;

    [ObservableProperty]
    private double totalReceivedThisMonth;

    [ObservableProperty]
    private int totalSourcesCount;

    [ObservableProperty]
    private int activeSourcesCount;

    [ObservableProperty]
    private int passiveSourcesCount;

    [ObservableProperty]
    private string currencySymbol = "₹";

    // Filtering
    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedTypeFilter = "ALL";

    public List<string> TypeFilterOptions { get; } = new() { "ALL", "ACTIVE", "PASSIVE" };

    public IncomeSourcesViewModel(
        IIncomeSourceService incomeSourceService,
        IAssetService assetService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _incomeSourceService = incomeSourceService;
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
        if (types.HasFlag(DataCacheType.IncomeSources) || types.HasFlag(DataCacheType.Assets))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var assets = _dataCacheService.CachedAssets;
        if (assets != null)
        {
            AvailableAssets = assets;
        }

        var sources = _dataCacheService.CachedIncomeSources;
        if (sources != null)
        {
            _allSources = new List<IncomeSource>(sources);

            // Compute summary metrics directly from cached items
            double activeSum = 0;
            double passiveSum = 0;
            int activeCount = 0;
            int passiveCount = 0;

            foreach (var s in _allSources)
            {
                var isPassive = string.Equals(s.Type, "PASSIVE", StringComparison.OrdinalIgnoreCase);
                var expected = s.ExpectedAmount ?? 0;
                var monthly = s.Frequency switch
                {
                    "DAILY" => expected * 30,
                    "WEEKLY" => expected * 4.33,
                    "YEARLY" => expected / 12.0,
                    _ => expected
                };

                if (isPassive)
                {
                    passiveSum += monthly;
                    passiveCount++;
                }
                else
                {
                    activeSum += monthly;
                    activeCount++;
                }
            }

            TotalActiveMonthly = activeSum;
            TotalPassiveMonthly = passiveSum;
            TotalExpectedMonthly = activeSum + passiveSum;
            TotalSourcesCount = _allSources.Count;
            ActiveSourcesCount = activeCount;
            PassiveSourcesCount = passiveCount;

            ApplyFilter();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedTypeFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadIncomeSourcesAsync()
    {
        if (_dataCacheService.CachedIncomeSources != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.IncomeSources | DataCacheType.Assets);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var sourcesTask = _dataCacheService.GetIncomeSourcesAsync();
            var assetsTask = _dataCacheService.GetAssetsAsync();

            await Task.WhenAll(sourcesTask, assetsTask);
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load income sources: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allSources.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            filtered = filtered.Where(s =>
                s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (s.Description != null && s.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (s.DefaultAssetName != null && s.DefaultAssetName.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(SelectedTypeFilter) && SelectedTypeFilter != "ALL")
        {
            filtered = filtered.Where(s => string.Equals(s.Type, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        IncomeSources.Clear();
        foreach (var s in filtered)
        {
            IncomeSources.Add(s);
        }
    }

    [RelayCommand]
    public async Task DeleteIncomeSourceAsync(IncomeSource? source)
    {
        if (source == null) return;

        if (string.Equals(source.Name, "Others", StringComparison.OrdinalIgnoreCase))
        {
            await _dialogService.ShowMessageAsync("Notice", "The default 'Others' income source cannot be deleted.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Income Source",
            $"Are you sure you want to delete '{source.Name}'? Any past transactions linked to it will be reassigned to 'Others'.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy("Deleting income source..."))
        {
            IsLoading = true;
            try
            {
                var res = await _incomeSourceService.DeleteIncomeSourceAsync(source.Id);
                if (res.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.IncomeSources);
                }
                else
                {
                    await _dialogService.ShowErrorAsync(res.Error ?? "Failed to delete income source.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"Exception: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
