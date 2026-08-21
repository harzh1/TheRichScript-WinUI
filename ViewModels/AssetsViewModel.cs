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

public class AssetGroup
{
    public string TypeName { get; set; } = string.Empty;
    public double TotalValue { get; set; }
    public double PercentageOfTotal { get; set; }
    public string TypeGlyph => TypeName.ToUpperInvariant() switch
    {
        "BANK" or "SAVINGS" or "CHECKING" => "\uE825",
        "INVESTMENT" or "STOCKS" or "MUTUAL FUNDS" or "EQUITY" => "\uEAFC",
        "CRYPTO" or "CRYPTO CURRENCY" or "BITCOIN" => "\uE945",
        "CASH" or "LIQUID CASH" or "WALLET" => "\uE1D0",
        "REAL ESTATE" or "PROPERTY" or "LAND" => "\uE80F",
        "RETIREMENT" or "EPF" or "PPF" or "PENSION" => "\uE819",
        "VEHICLE" or "GOLD" or "PRECIOUS METAL" => "\uE734",
        _ => "\uE77B"
    };
    public List<Asset> Items { get; set; } = new();
}

public partial class AssetsViewModel : ObservableObject
{
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    private List<Asset> _rawAssets = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Asset> allAssets = new();

    [ObservableProperty]
    private ObservableCollection<Asset> filteredAssets = new();

    [ObservableProperty]
    private ObservableCollection<AssetGroup> groupedAssets = new();

    [ObservableProperty]
    private ObservableCollection<string> assetTypes = new();

    [ObservableProperty]
    private double totalPortfolioValue;

    [ObservableProperty]
    private double liquidCashValue;

    [ObservableProperty]
    private double investedValue;

    [ObservableProperty]
    private int totalAssetCount;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedTypeFilter = "All Types";

    [ObservableProperty]
    private string currencySymbol = "₹";

    public AssetsViewModel(
        IAssetService assetService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
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
        if (types.HasFlag(DataCacheType.Assets) || types.HasFlag(DataCacheType.AssetTypes))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var types = _dataCacheService.CachedAssetTypes;
        if (types != null)
        {
            AssetTypes.Clear();
            AssetTypes.Add("All Types");
            foreach (var t in types)
            {
                AssetTypes.Add(t);
            }
        }

        var assets = _dataCacheService.CachedAssets;
        if (assets != null)
        {
            _rawAssets = new List<Asset>(assets);
            CalculatePortfolioMetrics();
            ApplyFilters();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedTypeFilterChanged(string value) => ApplyFilters();

    [RelayCommand]
    public async Task LoadAssetsAsync()
    {
        if (_dataCacheService.CachedAssets != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Assets | DataCacheType.AssetTypes);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var assetsTask = _dataCacheService.GetAssetsAsync();
            var typesTask = _dataCacheService.GetAssetTypesAsync();

            await Task.WhenAll(assetsTask, typesTask);
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading assets");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CalculatePortfolioMetrics()
    {
        double total = _rawAssets.Sum(a => a.Value);
        TotalPortfolioValue = total;
        TotalAssetCount = _rawAssets.Count;

        double liquid = 0;
        double invested = 0;

        foreach (var asset in _rawAssets)
        {
            asset.PercentageOfTotal = total > 0 ? (asset.Value / total) * 100 : 0;

            var t = (asset.Type ?? string.Empty).ToUpperInvariant();
            if (t is "BANK" or "SAVINGS" or "CHECKING" or "CASH" or "LIQUID CASH" or "WALLET")
            {
                liquid += asset.Value;
            }
            else
            {
                invested += asset.Value;
            }
        }

        LiquidCashValue = liquid;
        InvestedValue = invested;
    }

    private void ApplyFilters()
    {
        var query = _rawAssets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(a =>
                (a.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Type?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(SelectedTypeFilter) && SelectedTypeFilter != "All Types")
        {
            query = query.Where(a => string.Equals(a.Type, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = query.ToList();

        FilteredAssets.Clear();
        foreach (var asset in filteredList)
        {
            FilteredAssets.Add(asset);
        }

        GroupedAssets.Clear();
        var groups = filteredList
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Type) ? "OTHER" : a.Type.ToUpperInvariant())
            .OrderByDescending(g => g.Sum(a => a.Value));

        foreach (var g in groups)
        {
            double groupSum = g.Sum(a => a.Value);
            GroupedAssets.Add(new AssetGroup
            {
                TypeName = g.Key,
                TotalValue = groupSum,
                PercentageOfTotal = TotalPortfolioValue > 0 ? (groupSum / TotalPortfolioValue) * 100 : 0,
                Items = g.OrderByDescending(a => a.Value).ToList()
            });
        }
    }

    [RelayCommand]
    private async Task DeleteAssetAsync(Asset asset)
    {
        if (asset == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Asset",
            $"Are you sure you want to delete '{asset.Name}' with a balance of {CurrencySymbol}{asset.Value:N2}? Any linked transactions will be affected.",
            "Delete Asset",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy("Deleting asset account..."))
        {
            IsLoading = true;
            try
            {
                var response = await _assetService.DeleteAssetAsync(asset.Id);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                    await _dialogService.ShowMessageAsync("Success", $"Asset '{asset.Name}' has been deleted.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete asset.");
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
