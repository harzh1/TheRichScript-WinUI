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

public partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    private List<Transaction> _allTransactions = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Transaction> filteredTransactions = new();

    [ObservableProperty]
    private ObservableCollection<string> categories = new() { "ALL" };

    public List<string> TransactionTypes { get; } = new() { "ALL", "EXPENSE", "INCOME", "TRANSFER" };

    [ObservableProperty]
    private string selectedType = "ALL";

    [ObservableProperty]
    private string selectedCategory = "ALL";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? startDate;

    [ObservableProperty]
    private DateTimeOffset? endDate;

    [ObservableProperty]
    private double totalIncome;

    [ObservableProperty]
    private double totalExpense;

    [ObservableProperty]
    private double netBalance;

    [ObservableProperty]
    private string currencySymbol = "₹";

    public TransactionsViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAssetService assetService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
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
        if (types.HasFlag(DataCacheType.Transactions) ||
            types.HasFlag(DataCacheType.Categories) ||
            types.HasFlag(DataCacheType.Assets))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var assets = _dataCacheService.CachedAssets ?? new List<Asset>();
        var assetsMap = new Dictionary<string, string>();
        foreach (var asset in assets)
        {
            assetsMap[asset.Id] = asset.Name;
        }

        var cats = _dataCacheService.CachedCategories;
        if (cats != null)
        {
            Categories.Clear();
            Categories.Add("ALL");
            foreach (var cat in cats)
            {
                Categories.Add(cat.Name);
            }
        }

        var txs = _dataCacheService.CachedTransactions;
        if (txs != null)
        {
            foreach (var tx in txs)
            {
                var fromName = !string.IsNullOrEmpty(tx.AssetId) && assetsMap.TryGetValue(tx.AssetId, out var fName) ? fName : "Account";
                if (tx.IsTransfer && !string.IsNullOrEmpty(tx.ToAssetId))
                {
                    var toName = assetsMap.TryGetValue(tx.ToAssetId, out var tName) ? tName : "Account";
                    tx.AccountDisplay = $"{fromName} → {toName}";
                }
                else
                {
                    tx.AccountDisplay = fromName;
                }
            }
            _allTransactions = new List<Transaction>(txs);
            ApplyFilters();
        }
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        if (_dataCacheService.CachedTransactions != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Transactions | DataCacheType.Categories | DataCacheType.Assets);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var txTask = _dataCacheService.GetTransactionsAsync();
            var catTask = _dataCacheService.GetCategoriesAsync();
            var assetTask = _dataCacheService.GetAssetsAsync();

            await Task.WhenAll(txTask, catTask, assetTask);
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading transactions");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTypeChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnStartDateChanged(DateTimeOffset? value) => ApplyFilters();
    partial void OnEndDateChanged(DateTimeOffset? value) => ApplyFilters();

    private void ApplyFilters()
    {
        IEnumerable<Transaction> query = _allTransactions;

        if (!string.IsNullOrWhiteSpace(SelectedType) && SelectedType != "ALL")
        {
            query = query.Where(t => string.Equals(t.Type, SelectedType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "ALL")
        {
            query = query.Where(t => string.Equals(t.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (StartDate.HasValue)
        {
            var startUtc = StartDate.Value.Date;
            query = query.Where(t => t.Date.Date >= startUtc);
        }

        if (EndDate.HasValue)
        {
            var endUtc = EndDate.Value.Date;
            query = query.Where(t => t.Date.Date <= endUtc);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToLowerInvariant();
            query = query.Where(t =>
                (!string.IsNullOrEmpty(t.Description) && t.Description.ToLowerInvariant().Contains(search)) ||
                (!string.IsNullOrEmpty(t.Category) && t.Category.ToLowerInvariant().Contains(search)) ||
                t.Amount.ToString().Contains(search));
        }

        var list = query.ToList();

        FilteredTransactions.Clear();
        double inc = 0, exp = 0;

        foreach (var item in list)
        {
            FilteredTransactions.Add(item);
            if (item.IsIncome) inc += item.Amount;
            else if (item.IsExpense) exp += item.Amount;
        }

        TotalIncome = inc;
        TotalExpense = exp;
        NetBalance = inc - exp;
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(Transaction transaction)
    {
        if (transaction == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Transaction",
            $"Are you sure you want to delete this {transaction.Type.ToLower()} of {CurrencySymbol}{transaction.Amount:N2}? The associated asset balance will be reverted.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy("Deleting transaction..."))
        {
            IsLoading = true;
            try
            {
                var response = await _transactionService.DeleteTransactionAsync(transaction.Id);
                if (response.Success)
                {
                    _allTransactions.Remove(transaction);
                    ApplyFilters();
                    _dataCacheService.Invalidate(DataCacheType.Transactions | DataCacheType.Assets | DataCacheType.Analytics);
                    await _dialogService.ShowMessageAsync("Success", "Transaction deleted and asset balance reverted successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete transaction.");
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
