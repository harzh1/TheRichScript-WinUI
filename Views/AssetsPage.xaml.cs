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

public sealed partial class AssetsPage : Page
{
    public AssetsViewModel ViewModel { get; }

    public AssetsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<AssetsViewModel>();
        DataContext = ViewModel;

        Loaded += AssetsPage_Loaded;
    }

    private async void AssetsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAssetsAsync();
    }

    private async void AddAsset_Click(object sender, RoutedEventArgs e)
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
                    await ViewModel.LoadAssetsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to add asset.");
                }
            }
        }
    }

    private async void EditAsset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Asset asset)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var assetService = App.GetService<IAssetService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<string> types = dataCache?.CachedAssetTypes ?? (await assetService.GetAssetTypesAsync()).Data ?? new();

            var dialog = new AddAssetDialog(types, asset)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy("Updating asset account..."))
                {
                    var updateReq = new UpdateAssetRequest
                    {
                        Name = dialog.Result.Name,
                        Type = dialog.Result.Type,
                        Value = dialog.Result.Value,
                        Quantity = dialog.Result.Quantity,
                        Currency = !string.IsNullOrWhiteSpace(dialog.Result.Currency)
                            ? dialog.Result.Currency.ToUpperInvariant()
                            : (!string.IsNullOrWhiteSpace(asset.Currency) ? asset.Currency.ToUpperInvariant() : "INR")
                    };

                    var updateResp = await assetService.UpdateAssetAsync(asset.Id, updateReq);
                    if (updateResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Assets | DataCacheType.Analytics);
                        await ViewModel.LoadAssetsAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update asset.");
                    }
                }
            }
        }
    }

    private async void DeleteAsset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Asset asset)
        {
            await ViewModel.DeleteAssetCommand.ExecuteAsync(asset);
        }
    }

    private async void TransferFunds_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var assetService = App.GetService<IAssetService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        List<Asset> assets = dataCache?.CachedAssets ?? (await assetService.GetAssetsAsync()).Data ?? new();

        if (assets.Count < 2)
        {
            await dialogService.ShowErrorAsync("You need at least two assets to perform a transfer.");
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
                    await ViewModel.LoadAssetsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(transferResp.Error ?? "Transfer failed.");
                }
            }
        }
    }
}
