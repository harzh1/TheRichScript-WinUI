using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class IncomeSourcesPage : Page
{
    public IncomeSourcesViewModel ViewModel { get; }

    public IncomeSourcesPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<IncomeSourcesViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadIncomeSourcesAsync();
    }

    private async void AddIncomeSource_Click(object sender, RoutedEventArgs e)
    {
        var dialogService = App.GetService<IDialogService>();
        var incomeSourceService = App.GetService<IIncomeSourceService>();
        var dataCache = App.GetService<IDataCacheService>();
        var busy = App.GetService<IBusyIndicatorService>();

        var dialog = new AddIncomeSourceDialog(ViewModel.AvailableAssets)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.CreateResult != null)
        {
            using (busy?.BeginBusy("Adding income source..."))
            {
                ViewModel.IsLoading = true;
                try
                {
                    var response = await incomeSourceService.CreateIncomeSourceAsync(dialog.CreateResult);
                    if (response.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.IncomeSources);
                        await ViewModel.LoadIncomeSourcesAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(response.Error ?? "Failed to add income source");
                    }
                }
                catch (Exception ex)
                {
                    await dialogService.ShowErrorAsync($"Exception: {ex.Message}");
                }
                finally
                {
                    ViewModel.IsLoading = false;
                }
            }
        }
    }

    private async void EditIncomeSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is IncomeSource source)
        {
            var dialogService = App.GetService<IDialogService>();
            var incomeSourceService = App.GetService<IIncomeSourceService>();
            var dataCache = App.GetService<IDataCacheService>();
            var busy = App.GetService<IBusyIndicatorService>();

            var dialog = new AddIncomeSourceDialog(ViewModel.AvailableAssets, source)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.UpdateResult != null)
            {
                using (busy?.BeginBusy("Updating income source..."))
                {
                    ViewModel.IsLoading = true;
                    try
                    {
                        var response = await incomeSourceService.UpdateIncomeSourceAsync(source.Id, dialog.UpdateResult);
                        if (response.Success)
                        {
                            dataCache?.Invalidate(DataCacheType.IncomeSources);
                            await ViewModel.LoadIncomeSourcesAsync();
                        }
                        else
                        {
                            await dialogService.ShowErrorAsync(response.Error ?? "Failed to update income source");
                        }
                    }
                    catch (Exception ex)
                    {
                        await dialogService.ShowErrorAsync($"Exception: {ex.Message}");
                    }
                    finally
                    {
                        ViewModel.IsLoading = false;
                    }
                }
            }
        }
    }
}
