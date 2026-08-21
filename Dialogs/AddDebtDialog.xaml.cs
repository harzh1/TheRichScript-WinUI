using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddDebtDialog : ContentDialog
{
    public CreateDebtRequest? Result { get; private set; }

    public AddDebtDialog(List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        DatePicker.Date = DateTimeOffset.Now.AddMonths(1);

        PrimaryButtonClick += AddDebtDialog_PrimaryButtonClick;
    }

    private void AddDebtDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var person = PersonBox.Text.Trim();
        var amount = AmountNumberBox.Value;

        if (string.IsNullOrWhiteSpace(person) || double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            return;
        }

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            return;
        }

        var isLent = (TypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "LENT";

        Result = new CreateDebtRequest
        {
            PersonName = person,
            Amount = amount,
            AssetId = selectedAsset.Id,
            Type = isLent ? "LENT" : "BORROWED",
            DueDate = DatePicker.Date?.DateTime
        };
    }
}
