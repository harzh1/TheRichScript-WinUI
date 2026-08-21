using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel ViewModel { get; }

    public TransactionsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<TransactionsViewModel>();
        DataContext = ViewModel;

        Loaded += TransactionsPage_Loaded;
    }

    private async void TransactionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadTransactionsAsync();
    }

    private async void AddTransaction_Click(object sender, RoutedEventArgs e)
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
                    await ViewModel.LoadTransactionsAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create transaction.");
                }
            }
        }
    }

    private async void DeleteTx_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Transaction tx)
        {
            await ViewModel.DeleteTransactionCommand.ExecuteAsync(tx);
        }
    }

    private async void ViewReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string imgUrl && !string.IsNullOrWhiteSpace(imgUrl))
        {
            var image = new Image
            {
                Source = new BitmapImage(new Uri(imgUrl)),
                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                MaxWidth = 700,
                MaxHeight = 600
            };

            var dialog = new ContentDialog
            {
                Title = "Receipt Preview",
                Content = image,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default
            };

            await dialog.ShowAsync();
        }
    }
}
