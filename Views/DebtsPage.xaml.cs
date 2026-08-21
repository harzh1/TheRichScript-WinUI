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

public sealed partial class DebtsPage : Page
{
    public DebtsViewModel ViewModel { get; }

    public DebtsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<DebtsViewModel>();
        DataContext = ViewModel;

        Loaded += DebtsPage_Loaded;
    }

    private async void DebtsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDebtsAsync();
    }

    private async void AddDebt_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var debtSvc = App.GetService<IDebtService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();

        var dialog = new AddDebtDialog(assets)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Recording debt / loan..."))
            {
                var createResp = await debtSvc.CreateDebtAsync(dialog.Result);
                if (createResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Debts);
                    await ViewModel.LoadDebtsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create debt record.");
                }
            }
        }
    }

    private async void SettleDebt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Debt debt)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var debtService = App.GetService<IDebtService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();

            var dialog = new SettleDebtDialog(debt, assets)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy($"Processing settlement of {ViewModel.CurrencySymbol}{dialog.Result.Amount:N2}..."))
                {
                    var settleResp = await debtService.SettleDebtAsync(debt.Id, dialog.Result.Amount, dialog.Result.AssetId);
                    if (settleResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Debts | DataCacheType.Assets | DataCacheType.Transactions | DataCacheType.Analytics);
                        await ViewModel.LoadDebtsAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(settleResp.Error ?? "Settlement failed.");
                    }
                }
            }
        }
    }

    private async void WriteOffDebt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Debt debt)
        {
            await ViewModel.WriteOffDebtCommand.ExecuteAsync(debt);
        }
    }
}
