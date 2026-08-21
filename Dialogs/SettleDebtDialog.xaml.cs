using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class SettleDebtDialog : ContentDialog
{
    private readonly Debt _debt;
    public SettleDebtRequest? Result { get; private set; }

    public SettleDebtDialog(Debt debt, List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _debt = debt;

        var symbol = "₹";
        try
        {
            var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
            if (settings?.Settings?.CurrencySymbol != null) symbol = settings.Settings.CurrencySymbol;
        }
        catch { }

        DebtInfoText.Text = $"Person: {debt.PersonName}\nRemaining Balance: {symbol} {debt.RemainingAmount:N2} ({(debt.IsLent ? "Receivable from them" : "Payable to them")})";
        AssetLabel.Text = debt.IsLent ? "Account Receiving Payment" : "Account Paying Debt";

        AmountNumberBox.Maximum = debt.RemainingAmount;
        AmountNumberBox.Value = debt.RemainingAmount;

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        PrimaryButtonClick += SettleDebtDialog_PrimaryButtonClick;
    }

    private void SettleDebtDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0 || amount > _debt.RemainingAmount)
        {
            args.Cancel = true;
            return;
        }

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            return;
        }

        Result = new SettleDebtRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id
        };
    }
}
