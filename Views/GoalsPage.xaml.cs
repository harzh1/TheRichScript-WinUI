using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class GoalsPage : Page
{
    public GoalsViewModel ViewModel { get; }

    public GoalsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<GoalsViewModel>();
        DataContext = ViewModel;

        Loaded += GoalsPage_Loaded;
    }

    public static Visibility ShowFilterEmptyState(int totalCount, int filteredCount, bool isLoading) =>
        !isLoading && totalCount > 0 && filteredCount == 0 ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility ShowNoGoalsEmptyState(int totalCount, bool isLoading) =>
        !isLoading && totalCount == 0 ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility ShowGoalsList(int filteredCount) =>
        filteredCount > 0 ? Visibility.Visible : Visibility.Collapsed;

    private async void GoalsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadGoalsAsync();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearFilterCommand.Execute(null);
    }

    private async void AddGoal_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var goalService = App.GetService<IGoalService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();

        var dialog = new AddGoalDialog(assets)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Creating financial goal..."))
            {
                var createResp = await goalService.CreateGoalAsync(dialog.Result);
                if (createResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Goals);
                    await ViewModel.LoadGoalsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create goal.");
                }
            }
        }
    }

    private async void GoalCard_DepositFundsClicked(Goal goal)
    {
        if (goal == null) return;

        var dataCache = App.GetService<IDataCacheService>();
        var goalService = App.GetService<IGoalService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();

        if (assets.Count == 0)
        {
            await dialogService.ShowErrorAsync("You need at least one asset account with available funds to deposit.");
            return;
        }

        var dialog = new FundGoalDialog(goal, assets)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy($"Depositing {ViewModel.CurrencySymbol}{dialog.Result.Amount:N2} to '{goal.Name}'..."))
            {
                var fundResp = await goalService.AddFundsToGoalAsync(goal.Id, dialog.Result.Amount, dialog.Result.AssetId);
                if (fundResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Goals | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                    await ViewModel.LoadGoalsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(fundResp.Error ?? "Failed to deposit funds.");
                }
            }
        }
    }

    private async void GoalCard_TogglePinClicked(Goal goal)
    {
        if (goal != null)
        {
            await ViewModel.TogglePinCommand.ExecuteAsync(goal);
        }
    }

    private async void GoalCard_EditGoalClicked(Goal goal)
    {
        if (goal == null) return;

        var dataCache = App.GetService<IDataCacheService>();
        var goalService = App.GetService<IGoalService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();

        var dialog = new AddGoalDialog(assets, goal)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.UpdateResult != null)
        {
            using (busy?.BeginBusy($"Updating goal '{goal.Name}'..."))
            {
                var updateResp = await goalService.UpdateGoalAsync(goal.Id, dialog.UpdateResult, dialog.ImageFilePaths);
                if (updateResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Goals);
                    await ViewModel.LoadGoalsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update goal.");
                }
            }
        }
    }

    private async void GoalCard_DeleteGoalClicked(Goal goal)
    {
        if (goal != null)
        {
            await ViewModel.DeleteGoalCommand.ExecuteAsync(goal);
        }
    }
}
