using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class BudgetsPlansViewModel : ObservableObject
{
    private readonly IPlanService _planService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<PlannedExpense> recurringPlans = new();

    [ObservableProperty]
    private ObservableCollection<PlannedExpense> manualPlans = new();

    [ObservableProperty]
    private double totalMonthlyCommitments;

    [ObservableProperty]
    private double totalRecurringIncome;

    [ObservableProperty]
    private double netRecurringCashflow;

    [ObservableProperty]
    private int activePlansCount;

    [ObservableProperty]
    private string currencySymbol = "₹";

    public BudgetsPlansViewModel(
        IPlanService planService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _planService = planService;
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
        if (types.HasFlag(DataCacheType.Plans))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var plans = _dataCacheService.CachedPlans;
        if (plans != null)
        {
            RecurringPlans.Clear();
            ManualPlans.Clear();
            double recExp = 0, recInc = 0;

            foreach (var plan in plans)
            {
                if (plan.IsRecurring)
                {
                    RecurringPlans.Add(plan);
                    if (plan.IsIncome) recInc += plan.Amount;
                    else recExp += plan.Amount;
                }
                else
                {
                    ManualPlans.Add(plan);
                }
            }

            TotalMonthlyCommitments = recExp;
            TotalRecurringIncome = recInc;
            NetRecurringCashflow = recInc - recExp;
            ActivePlansCount = RecurringPlans.Count + ManualPlans.Count;
        }
    }

    [RelayCommand]
    public async Task LoadPlansAsync()
    {
        if (_dataCacheService.CachedPlans != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Plans);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await _dataCacheService.GetPlansAsync();
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading plans");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeletePlanAsync(PlannedExpense plan)
    {
        if (plan == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Plan",
            $"Are you sure you want to delete planned expense '{plan.Name}' of {CurrencySymbol}{plan.Amount:N2}?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy("Deleting planned expense..."))
        {
            IsLoading = true;
            try
            {
                var response = await _planService.DeletePlanAsync(plan.Id);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Plans);
                    await _dialogService.ShowMessageAsync("Success", $"Plan '{plan.Name}' deleted.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete plan.");
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
