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

public partial class DebtsViewModel : ObservableObject
{
    private readonly IDebtService _debtService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;
    private readonly IFxRateService _fxRateService;

    private List<Debt> _rawLentDebts = new();
    private List<Debt> _rawBorrowedDebts = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Debt> filteredLentDebts = new();

    [ObservableProperty]
    private ObservableCollection<Debt> filteredBorrowedDebts = new();

    [ObservableProperty]
    private bool showSettledAndWrittenOff = false;

    [ObservableProperty]
    private double totalLentAmount;

    [ObservableProperty]
    private double totalLentPending;

    [ObservableProperty]
    private double totalBorrowedAmount;

    [ObservableProperty]
    private double totalBorrowedPending;

    [ObservableProperty]
    private double netReceivable;

    [ObservableProperty]
    private int totalDebtsCount;

    [ObservableProperty]
    private string currencySymbol = "₹";

    public DebtsViewModel(
        IDebtService debtService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService,
        IFxRateService fxRateService)
    {
        _debtService = debtService;
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
        if (types.HasFlag(DataCacheType.Debts))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var debts = _dataCacheService.CachedDebts;
        if (debts != null)
        {
            _rawLentDebts.Clear();
            _rawBorrowedDebts.Clear();

            var baseCurrency = _settingsService.Settings.PreferredCurrency ?? "INR";
            double lentTotal = 0, lentPending = 0;
            double borrowedTotal = 0, borrowedPending = 0;

            foreach (var debt in debts)
            {
                var debtCurr = string.IsNullOrWhiteSpace(debt.Currency) ? baseCurrency : debt.Currency;
                var convertedTotal = _fxRateService.ConvertAmount(debt.Amount, debtCurr, baseCurrency);
                var convertedPending = _fxRateService.ConvertAmount(debt.RemainingAmount, debtCurr, baseCurrency);

                debt.BaseCurrency = baseCurrency;
                debt.BaseAmount = convertedTotal;
                debt.BaseRemainingAmount = convertedPending;

                if (debt.IsLent)
                {
                    _rawLentDebts.Add(debt);
                    lentTotal += convertedTotal;
                    if (!debt.IsSettled && !debt.IsExpired)
                    {
                        lentPending += convertedPending;
                    }
                }
                else
                {
                    _rawBorrowedDebts.Add(debt);
                    borrowedTotal += convertedTotal;
                    if (!debt.IsSettled && !debt.IsExpired)
                    {
                        borrowedPending += convertedPending;
                    }
                }
            }

            TotalLentAmount = lentTotal;
            TotalLentPending = lentPending;
            TotalBorrowedAmount = borrowedTotal;
            TotalBorrowedPending = borrowedPending;
            NetReceivable = lentPending - borrowedPending;
            TotalDebtsCount = _rawLentDebts.Count + _rawBorrowedDebts.Count;

            ApplyFilters();

            // Asynchronously preload live rates if foreign currencies are present
            var foreignCurrencies = debts
                .Select(d => d.Currency)
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

    partial void OnShowSettledAndWrittenOffChanged(bool value) => ApplyFilters();

    [RelayCommand]
    public async Task LoadDebtsAsync()
    {
        if (_dataCacheService.CachedDebts != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Debts);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await _dataCacheService.GetDebtsAsync();
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading debts");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        FilteredLentDebts.Clear();
        var lentQuery = ShowSettledAndWrittenOff
            ? _rawLentDebts
            : _rawLentDebts.Where(d => !d.IsSettled && !d.IsExpired);

        foreach (var debt in lentQuery)
        {
            FilteredLentDebts.Add(debt);
        }

        FilteredBorrowedDebts.Clear();
        var borrowedQuery = ShowSettledAndWrittenOff
            ? _rawBorrowedDebts
            : _rawBorrowedDebts.Where(d => !d.IsSettled && !d.IsExpired);

        foreach (var debt in borrowedQuery)
        {
            FilteredBorrowedDebts.Add(debt);
        }
    }

    [RelayCommand]
    private async Task WriteOffDebtAsync(Debt debt)
    {
        if (debt == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Write Off Debt",
            $"Are you sure you want to write off the remaining {CurrencySymbol}{debt.RemainingAmount:N2} for {debt.PersonName}? This will mark the debt as expired without altering your asset balance.",
            "Write Off",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy($"Writing off debt for {debt.PersonName}..."))
        {
            IsLoading = true;
            try
            {
                var response = await _debtService.WriteOffDebtAsync(debt.Id);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Debts);
                    await _dialogService.ShowMessageAsync("Success", $"Debt for {debt.PersonName} has been written off.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to write off debt.");
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
