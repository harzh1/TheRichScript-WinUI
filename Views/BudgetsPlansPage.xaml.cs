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

public sealed partial class BudgetsPlansPage : Page
{
    public BudgetsPlansViewModel ViewModel { get; }

    public BudgetsPlansPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<BudgetsPlansViewModel>();
        DataContext = ViewModel;

        Loaded += BudgetsPlansPage_Loaded;
    }

    private async void BudgetsPlansPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPlansAsync();
    }

    private async void AddSubscription_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var planService = App.GetService<IPlanService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
        List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

        var dialog = new AddSubscriptionDialog(assets, cats)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Creating subscription..."))
            {
                var createResp = await planService.CreatePlanAsync(dialog.Result);
                if (createResp.Success)
                {
                    if (dialog.Result.NextDueDate.HasValue && dialog.Result.NextDueDate.Value.Date <= DateTime.Today)
                    {
                        await planService.TriggerRecurringPlansAsync();
                        dataCache?.Invalidate(DataCacheType.Plans | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                    }
                    else
                    {
                        dataCache?.Invalidate(DataCacheType.Plans);
                    }
                    await ViewModel.LoadPlansAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create subscription.");
                }
            }
        }
    }

    private async void AddRecurringPlan_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var planService = App.GetService<IPlanService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
        List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

        var dialog = new AddRecurringPlanDialog(assets, cats)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Creating recurring plan..."))
            {
                var createResp = await planService.CreatePlanAsync(dialog.Result);
                if (createResp.Success)
                {
                    if (dialog.Result.NextDueDate.HasValue && dialog.Result.NextDueDate.Value.Date <= DateTime.Today)
                    {
                        await planService.TriggerRecurringPlansAsync();
                        dataCache?.Invalidate(DataCacheType.Plans | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                    }
                    else
                    {
                        dataCache?.Invalidate(DataCacheType.Plans);
                    }
                    await ViewModel.LoadPlansAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create recurring plan.");
                }
            }
        }
    }

    private async void AddQuickAdd_Click(object sender, RoutedEventArgs e)
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
            using (busy?.BeginBusy("Creating template..."))
            {
                var createResp = await planService.CreatePlanAsync(dialog.Result);
                if (createResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Plans);
                    await ViewModel.LoadPlansAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create template.");
                }
            }
        }
    }

    private async void EditSubscription_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var planService = App.GetService<IPlanService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
            List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

            var dialog = new AddSubscriptionDialog(assets, cats, existingPlan: plan)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Updating subscription..."))
                {
                    var updateReq = new UpdatePlanRequest
                    {
                        Name = dialog.Result.Name,
                        Amount = dialog.Result.Amount,
                        AssetId = dialog.Result.AssetId,
                        TargetAssetId = null,
                        Category = dialog.Result.Category,
                        TxType = "EXPENSE",
                        Frequency = dialog.Result.Frequency,
                        NextDueDate = dialog.Result.NextDueDate
                    };

                    var updateResp = await planService.UpdatePlanAsync(plan.Id, updateReq);
                    if (updateResp.Success)
                    {
                        if (updateReq.NextDueDate.HasValue && updateReq.NextDueDate.Value.Date <= DateTime.Today)
                        {
                            await planService.TriggerRecurringPlansAsync();
                            dataCache?.Invalidate(DataCacheType.Plans | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                        }
                        else
                        {
                            dataCache?.Invalidate(DataCacheType.Plans);
                        }
                        await ViewModel.LoadPlansAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update subscription.");
                    }
                }
            }
        }
    }

    private async void EditRecurringPlan_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var planService = App.GetService<IPlanService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
            List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

            var dialog = new AddRecurringPlanDialog(assets, cats, existingPlan: plan)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Updating recurring plan..."))
                {
                    var updateReq = new UpdatePlanRequest
                    {
                        Name = dialog.Result.Name,
                        Amount = dialog.Result.Amount,
                        AssetId = dialog.Result.AssetId,
                        TargetAssetId = dialog.Result.TargetAssetId,
                        Category = dialog.Result.Category,
                        TxType = "TRANSFER",
                        Frequency = dialog.Result.Frequency,
                        NextDueDate = dialog.Result.NextDueDate
                    };

                    var updateResp = await planService.UpdatePlanAsync(plan.Id, updateReq);
                    if (updateResp.Success)
                    {
                        if (updateReq.NextDueDate.HasValue && updateReq.NextDueDate.Value.Date <= DateTime.Today)
                        {
                            await planService.TriggerRecurringPlansAsync();
                            dataCache?.Invalidate(DataCacheType.Plans | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                        }
                        else
                        {
                            dataCache?.Invalidate(DataCacheType.Plans);
                        }
                        await ViewModel.LoadPlansAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update recurring plan.");
                    }
                }
            }
        }
    }

    private async void EditTemplate_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
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
                using (busy?.BeginBusy("Updating template..."))
                {
                    var updateReq = new UpdatePlanRequest
                    {
                        Name = dialog.Result.Name,
                        Amount = dialog.Result.Amount,
                        AssetId = dialog.Result.AssetId,
                        TargetAssetId = null,
                        Category = dialog.Result.Category,
                        TxType = dialog.Result.TxType,
                        Frequency = null,
                        NextDueDate = null
                    };

                    var updateResp = await planService.UpdatePlanAsync(plan.Id, updateReq);
                    if (updateResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Plans);
                        await ViewModel.LoadPlansAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update template.");
                    }
                }
            }
        }
    }

    private async void DeletePlan_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PlannedExpense plan)
        {
            await ViewModel.DeletePlanCommand.ExecuteAsync(plan);
        }
    }

    private async void ProcessDueTransfers_Click(object sender, RoutedEventArgs e)
    {
        var planService = App.GetService<IPlanService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();
        var dataCache = App.GetService<IDataCacheService>();

        using (busy?.BeginBusy("Processing due recurring plans & subscriptions..."))
        {
            var resp = await planService.TriggerRecurringPlansAsync();
            if (resp.Success)
            {
                dataCache?.Invalidate(DataCacheType.Plans | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                await ViewModel.LoadPlansAsync();
                await dialogService.ShowMessageAsync("Sync Complete", "Due recurring transfers and subscriptions have been processed successfully.");
            }
            else
            {
                await dialogService.ShowErrorAsync(resp.Error ?? "Failed to process recurring plans.");
            }
        }
    }
}
