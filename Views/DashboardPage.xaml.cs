using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class DashboardPage : Page
{
    private bool _isDialogOpen;
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<DashboardViewModel>();
        DataContext = ViewModel;

        Loaded += DashboardPage_Loaded;
    }

    private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDashboardDataAsync();
    }

    private async void QuickAddTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        _isDialogOpen = true;

        try
        {
            var dataCache = App.GetService<IDataCacheService>();
            var txService = App.GetService<ITransactionService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
            List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();
            List<IncomeSource> incomeSources = dataCache?.CachedIncomeSources ?? (await App.GetService<IIncomeSourceService>().GetIncomeSourcesAsync()).Data ?? new();

            var dialog = new AddTransactionDialog(assets, cats, incomeSources)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Adding transaction..."))
                {
                    var createResp = await txService.CreateTransactionAsync(dialog.Result);
                    if (createResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Transactions | DataCacheType.Assets | DataCacheType.Analytics);
                        await ViewModel.LoadDashboardDataAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create transaction.");
                    }
                }
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private async void QuickTransfer_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        _isDialogOpen = true;

        try
        {
            var dataCache = App.GetService<IDataCacheService>();
            var assetService = App.GetService<IAssetService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await assetService.GetAssetsAsync()).Data ?? new();

            if (assets.Count < 2)
            {
                await dialogService.ShowErrorAsync("You need at least two assets/accounts to perform a fund transfer.");
                return;
            }

            var dialog = new TransferAssetDialog(assets)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Processing fund transfer..."))
                {
                    var transferResp = await assetService.TransferAssetAsync(dialog.Result);
                    if (transferResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                        await ViewModel.LoadDashboardDataAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(transferResp.Error ?? "Transfer failed.");
                    }
                }
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private async void QuickAddAsset_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        _isDialogOpen = true;

        try
        {
            var dataCache = App.GetService<IDataCacheService>();
            var assetService = App.GetService<IAssetService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<string> types = dataCache?.CachedAssetTypes ?? (await assetService.GetAssetTypesAsync()).Data ?? new();

            var dialog = new AddAssetDialog(types)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Adding asset account..."))
                {
                    var createResp = await assetService.CreateAssetAsync(dialog.Result);
                    if (createResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Assets | DataCacheType.Analytics);
                        await ViewModel.LoadDashboardDataAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to add asset.");
                    }
                }
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private async void NewQuickAddTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        _isDialogOpen = true;

        try
        {
            var dataCache = App.GetService<IDataCacheService>();
            var planService = App.GetService<IPlanService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
            List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

            var dialog = new AddPlanDialog(assets, cats, defaultType: "MANUAL")
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Creating Quick Add template..."))
                {
                    var resp = await planService.CreatePlanAsync(dialog.Result);
                    if (resp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Plans);
                        await ViewModel.LoadDashboardDataAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(resp.Error ?? "Failed to create Quick Add template.");
                    }
                }
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private async void EditQuickAddTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;

        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
        {
            _isDialogOpen = true;
            try
            {
                var dataCache = App.GetService<IDataCacheService>();
                var planService = App.GetService<IPlanService>();
                var dialogService = App.GetService<IDialogService>();
                var busy = App.GetService<IBusyIndicatorService>();

                List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
                List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

                var dialog = new AddPlanDialog(assets, cats, defaultType: "MANUAL", existingPlan: plan)
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    using (busy?.BeginBusy("Updating Quick Add template..."))
                    {
                        var updateReq = new UpdatePlanRequest
                        {
                            Name = dialog.Result.Name,
                            Amount = dialog.Result.Amount,
                            Category = dialog.Result.Category,
                            AssetId = dialog.Result.AssetId,
                            TxType = dialog.Result.TxType
                        };

                        var resp = await planService.UpdatePlanAsync(plan.Id, updateReq);
                        if (resp.Success)
                        {
                            dataCache?.Invalidate(DataCacheType.Plans);
                            await ViewModel.LoadDashboardDataAsync();
                        }
                        else
                        {
                            await dialogService.ShowErrorAsync(resp.Error ?? "Failed to update template.");
                        }
                    }
                }
            }
            catch { }
            finally
            {
                _isDialogOpen = false;
            }
        }
    }

    private async void DeleteQuickAddTemplate_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
        {
            await ViewModel.DeleteQuickAddTemplateAsync(plan);
        }
    }

    private async void LogQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PlannedExpense plan)
        {
            if (btn.Parent is StackPanel panel)
            {
                var numBox = panel.FindName("QuickAddAmountBox") as NumberBox;
                if (numBox != null && !double.IsNaN(numBox.Value) && numBox.Value > 0)
                {
                    plan.Amount = numBox.Value;
                }

                var assetCmb = panel.FindName("QuickAddAssetComboBox") as ComboBox;
                if (assetCmb?.SelectedValue is string selectedAssetId && !string.IsNullOrEmpty(selectedAssetId))
                {
                    plan.AssetId = selectedAssetId;
                }
            }

            await ViewModel.LogQuickAddTransactionAsync(plan);
        }
    }
}
