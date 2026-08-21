using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<Category> categories = new();

    [ObservableProperty]
    private int totalCategoriesCount;

    [ObservableProperty]
    private int budgetedCategoriesCount;

    [ObservableProperty]
    private double totalMonthlyBudgetCap;

    [ObservableProperty]
    private double currentMonthSpending;

    [ObservableProperty]
    private string currencySymbol = "₹";

    public CategoriesViewModel(
        ICategoryService categoryService,
        IAnalyticsService analyticsService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _categoryService = categoryService;
        _analyticsService = analyticsService;
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
        if (types.HasFlag(DataCacheType.Categories) || types.HasFlag(DataCacheType.Analytics))
        {
            ApplyCachedData();
        }
    }

    private void ApplyCachedData()
    {
        var catList = _dataCacheService.CachedCategories;
        if (catList != null)
        {
            Categories.Clear();

            int budgetedCount = 0;
            double budgetCap = 0;
            double totalSpent = 0;

            var expensesByCategory = _dataCacheService.CachedAnalytics?.ExpensesByCategory;

            foreach (var cat in catList)
            {
                double spent = 0;
                if (expensesByCategory != null && expensesByCategory.TryGetValue(cat.Name, out var expData))
                {
                    spent = expData.Monthly;
                }

                cat.MonthlySpent = spent;
                Categories.Add(cat);

                if (cat.MonthlyLimit.HasValue && cat.MonthlyLimit.Value > 0)
                {
                    budgetedCount++;
                    budgetCap += cat.MonthlyLimit.Value;
                    totalSpent += spent;
                }
            }

            TotalCategoriesCount = catList.Count;
            BudgetedCategoriesCount = budgetedCount;
            TotalMonthlyBudgetCap = budgetCap;
            CurrentMonthSpending = totalSpent;
        }
    }

    [RelayCommand]
    public async Task LoadCategoriesAsync()
    {
        if (_dataCacheService.CachedCategories != null)
        {
            ApplyCachedData();
            IsLoading = false;

            // Silently revalidate in background
            _ = _dataCacheService.RevalidateAsync(DataCacheType.Categories | DataCacheType.Analytics);
            return;
        }

        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var catTask = _dataCacheService.GetCategoriesAsync();
            var analyticsTask = _dataCacheService.GetAnalyticsSummaryAsync();

            await Task.WhenAll(catTask, analyticsTask);
            ApplyCachedData();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error loading categories");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category category)
    {
        if (category == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Category",
            $"Are you sure you want to delete category '{category.Name}'? Note: If this category is linked to existing transactions or plans, deletion will be safely rejected.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        using (_busyIndicatorService.BeginBusy($"Deleting category '{category.Name}'..."))
        {
            IsLoading = true;
            try
            {
                var response = await _categoryService.DeleteCategoryAsync(category.Id);
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.Categories);
                    await _dialogService.ShowMessageAsync("Success", $"Category '{category.Name}' deleted.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete category.");
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
