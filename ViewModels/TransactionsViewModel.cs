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
    [NotifyPropertyChangedFor(nameof(ShowEndOfTransactions))]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEndOfTransactions))]
    private bool isLoadingMore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEndOfTransactions))]
    private bool hasMoreTransactions;

    public bool ShowEndOfTransactions => !HasMoreTransactions && FilteredTransactions.Count > 0 && !IsLoading && !IsLoadingMore;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int pageSize = 30;

    [ObservableProperty]
    private int totalCount;

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

        LoadCategories();
    }

    private void OnDataUpdated(DataCacheType types)
    {
        if (types.HasFlag(DataCacheType.Categories))
        {
            LoadCategories();
        }

        if (types.HasFlag(DataCacheType.Transactions))
        {
            _ = LoadTransactionsAsync(resetPage: true);
        }
    }

    private void LoadCategories()
    {
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
    }

    private void PopulateAccountDisplays(IEnumerable<Transaction> txs)
    {
        var assets = _dataCacheService.CachedAssets ?? new List<Asset>();
        var assetsMap = new Dictionary<string, string>();
        foreach (var asset in assets)
        {
            assetsMap[asset.Id] = asset.Name;
        }

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
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync(bool resetPage = true)
    {
        if (IsLoading) return;
        IsLoading = true;

        if (resetPage)
        {
            CurrentPage = 1;
            HasMoreTransactions = true;
        }

        try
        {
            LoadCategories();

            var res = await _transactionService.GetTransactionsAsync(
                category: SelectedCategory,
                type: SelectedType,
                startDate: StartDate?.UtcDateTime,
                endDate: EndDate?.UtcDateTime,
                page: CurrentPage,
                limit: PageSize,
                search: SearchText);

            if (res.Success && res.Data != null)
            {
                PopulateAccountDisplays(res.Data);

                if (resetPage)
                {
                    FilteredTransactions.Clear();
                }

                foreach (var tx in res.Data)
                {
                    FilteredTransactions.Add(tx);
                }

                if (res.Meta != null)
                {
                    TotalCount = res.Meta.Total;
                    HasMoreTransactions = res.Meta.HasMore;
                }
                else
                {
                    HasMoreTransactions = res.Data.Count >= PageSize;
                }

                UpdateSummaryTotals();
                OnPropertyChanged(nameof(ShowEndOfTransactions));
            }
            else if (!string.IsNullOrEmpty(res.Error))
            {
                await _dialogService.ShowErrorAsync(res.Error, "Error loading transactions");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading transactions");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEndOfTransactions));
        }
    }

    [RelayCommand]
    public async Task LoadMoreTransactionsAsync()
    {
        if (IsLoading || IsLoadingMore || !HasMoreTransactions) return;
        IsLoadingMore = true;

        var nextPage = CurrentPage + 1;

        try
        {
            var res = await _transactionService.GetTransactionsAsync(
                category: SelectedCategory,
                type: SelectedType,
                startDate: StartDate?.UtcDateTime,
                endDate: EndDate?.UtcDateTime,
                page: nextPage,
                limit: PageSize,
                search: SearchText);

            if (res.Success && res.Data != null)
            {
                PopulateAccountDisplays(res.Data);

                foreach (var tx in res.Data)
                {
                    FilteredTransactions.Add(tx);
                }

                CurrentPage = nextPage;

                if (res.Meta != null)
                {
                    TotalCount = res.Meta.Total;
                    HasMoreTransactions = res.Meta.HasMore;
                }
                else
                {
                    HasMoreTransactions = res.Data.Count >= PageSize;
                }

                UpdateSummaryTotals();
                OnPropertyChanged(nameof(ShowEndOfTransactions));
            }
            else
            {
                HasMoreTransactions = false;
            }
        }
        catch
        {
            HasMoreTransactions = false;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    partial void OnSelectedTypeChanged(string value) => _ = LoadTransactionsAsync(resetPage: true);
    partial void OnSelectedCategoryChanged(string value) => _ = LoadTransactionsAsync(resetPage: true);
    partial void OnSearchTextChanged(string value) => _ = LoadTransactionsAsync(resetPage: true);
    partial void OnStartDateChanged(DateTimeOffset? value) => _ = LoadTransactionsAsync(resetPage: true);
    partial void OnEndDateChanged(DateTimeOffset? value) => _ = LoadTransactionsAsync(resetPage: true);

    private void UpdateSummaryTotals()
    {
        double inc = 0, exp = 0;
        foreach (var tx in FilteredTransactions)
        {
            if (tx.IsIncome) inc += tx.Amount;
            else if (tx.IsExpense) exp += tx.Amount;
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
                    FilteredTransactions.Remove(transaction);
                    UpdateSummaryTotals();
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
