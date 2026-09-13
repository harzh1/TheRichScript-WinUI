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

        var contactSvc = App.GetService<IContactService>();
        List<Asset> assets = dataCache?.CachedAssets ?? (await App.GetService<IAssetService>().GetAssetsAsync()).Data ?? new();
        List<Contact> contacts = new();
        if (contactSvc != null)
        {
            try
            {
                var contactsResp = await contactSvc.GetContactsAsync();
                if (contactsResp.Success && contactsResp.Data != null)
                {
                    contacts = contactsResp.Data;
                }
            }
            catch { }
        }

        var dialog = new AddDebtDialog(assets, contacts)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Recording debt / loan..."))
            {
                // If contact is not yet created, auto-create it via Contacts API
                if (string.IsNullOrEmpty(dialog.Result.ContactId) && !string.IsNullOrWhiteSpace(dialog.Result.PersonName) && contactSvc != null)
                {
                    try
                    {
                        var createContactResp = await contactSvc.CreateContactAsync(new CreateContactRequest
                        {
                            Name = dialog.Result.PersonName,
                            Notes = dialog.Result.Notes
                        });
                        if (createContactResp.Success && createContactResp.Data != null)
                        {
                            dialog.Result.ContactId = createContactResp.Data.Id;
                            dataCache?.Invalidate(DataCacheType.Contacts);
                        }
                    }
                    catch { }
                }

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
                using (busy?.BeginBusy($"Processing settlement of {debt.CurrencySymbol}{dialog.Result.Amount:N2}..."))
                {
                    var settleResp = await debtService.SettleDebtAsync(debt.Id, dialog.Result);
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

    private async void EditDebt_Click(object sender, RoutedEventArgs e)
    {
        var debt = (sender as FrameworkElement)?.Tag as Debt;
        if (debt != null)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var debtService = App.GetService<IDebtService>();
            var assetService = App.GetService<IAssetService>();
            var contactService = App.GetService<IContactService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            List<Asset> assets = dataCache?.CachedAssets ?? (await assetService.GetAssetsAsync()).Data ?? new();
            List<Contact> contacts = new();
            if (contactService != null)
            {
                try
                {
                    var contactsResp = await contactService.GetContactsAsync();
                    if (contactsResp.Success && contactsResp.Data != null)
                    {
                        contacts = contactsResp.Data;
                    }
                }
                catch { }
            }

            var dialog = new AddDebtDialog(assets, contacts, debt)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.UpdateResult != null)
            {
                using (busy?.BeginBusy($"Updating debt with {debt.PersonName}..."))
                {
                    ViewModel.IsLoading = true;
                    try
                    {
                        var updateResp = await debtService.UpdateDebtAsync(debt.Id, dialog.UpdateResult);
                        if (updateResp.Success)
                        {
                            dataCache?.Invalidate(DataCacheType.Debts | DataCacheType.Assets | DataCacheType.Analytics);
                            await ViewModel.LoadDebtsAsync();
                        }
                        else
                        {
                            await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update debt.");
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

    private async void WriteOffDebt_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Debt debt)
        {
            await ViewModel.WriteOffDebtCommand.ExecuteAsync(debt);
        }
    }
}
