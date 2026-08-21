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

    private async void AddPlan_Click(object sender, RoutedEventArgs e)
    {
        await OpenPlanDialogAsync("RECURRING");
    }

    private async void AddQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        await OpenPlanDialogAsync("MANUAL");
    }

    private async Task OpenPlanDialogAsync(string defaultType)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var planService = App.GetService<IPlanService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
        List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

        var dialog = new AddPlanDialog(assets, cats, defaultType: defaultType)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Creating planned expense..."))
            {
                var createResp = await planService.CreatePlanAsync(dialog.Result);
                if (createResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Plans);
                    await ViewModel.LoadPlansAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create plan.");
                }
            }
        }
    }

    private async void EditPlan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PlannedExpense plan)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var planService = App.GetService<IPlanService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
            List<string> cats = dataCache?.CachedCategories?.Select(c => c.Name).ToList() ?? (await App.GetService<ICategoryService>().GetCategoriesAsync()).Data?.Select(c => c.Name).ToList() ?? new();

            var dialog = new AddPlanDialog(assets, cats, existingPlan: plan)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Updating planned expense..."))
                {
                    var updateReq = new UpdatePlanRequest
                    {
                        Name = dialog.Result.Name,
                        Amount = dialog.Result.Amount,
                        AssetId = dialog.Result.AssetId,
                        Category = dialog.Result.Category,
                        TxType = dialog.Result.TxType,
                        Frequency = dialog.Result.Frequency,
                        NextDueDate = dialog.Result.NextDueDate
                    };

                    var updateResp = await planService.UpdatePlanAsync(plan.Id, updateReq);
                    if (updateResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Plans);
                        await ViewModel.LoadPlansAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update plan.");
                    }
                }
            }
        }
    }

    private async void DeletePlan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PlannedExpense plan)
        {
            await ViewModel.DeletePlanCommand.ExecuteAsync(plan);
        }
    }
}
