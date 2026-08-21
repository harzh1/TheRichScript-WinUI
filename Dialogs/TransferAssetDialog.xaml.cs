using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class TransferAssetDialog : ContentDialog
{
    public TransferAssetRequest? Result { get; private set; }

    public TransferAssetDialog(List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        FromAssetComboBox.ItemsSource = assets;
        ToAssetComboBox.ItemsSource = assets;

        if (assets.Count > 0)
        {
            FromAssetComboBox.SelectedIndex = 0;
            if (assets.Count > 1)
            {
                ToAssetComboBox.SelectedIndex = 1;
            }
        }

        DatePicker.Date = DateTimeOffset.Now;

        PrimaryButtonClick += TransferAssetDialog_PrimaryButtonClick;
    }

    private void TransferAssetDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (FromAssetComboBox.SelectedItem is not Asset fromAsset ||
            ToAssetComboBox.SelectedItem is not Asset toAsset)
        {
            args.Cancel = true;
            return;
        }

        if (fromAsset.Id == toAsset.Id)
        {
            args.Cancel = true;
            return;
        }

        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0 || fromAsset.Value < amount)
        {
            args.Cancel = true;
            return;
        }

        Result = new TransferAssetRequest
        {
            FromAssetId = fromAsset.Id,
            ToAssetId = toAsset.Id,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? $"Transfer to {toAsset.Name}" : DescriptionBox.Text.Trim(),
            Date = DatePicker.Date?.DateTime ?? DateTime.Now,
            Category = "TRANSFER"
        };
    }
}
