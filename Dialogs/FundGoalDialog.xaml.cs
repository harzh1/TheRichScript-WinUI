using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class FundGoalDialog : ContentDialog
{
    private readonly Goal _goal;
    public AddGoalFundsRequest? Result { get; private set; }

    public FundGoalDialog(Goal goal, List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _goal = goal;

        var symbol = "₹";
        try
        {
            var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
            if (settings?.Settings?.CurrencySymbol != null) symbol = settings.Settings.CurrencySymbol;
        }
        catch { }

        GoalInfoText.Text = $"Goal: {goal.Name}\nRemaining Target: {symbol} {goal.RemainingAmount:N2}";
        AmountNumberBox.Maximum = goal.RemainingAmount;
        AmountNumberBox.Value = goal.RemainingAmount;

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        PrimaryButtonClick += FundGoalDialog_PrimaryButtonClick;
    }

    private void FundGoalDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            return;
        }

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            return;
        }

        if (selectedAsset.Value < amount)
        {
            args.Cancel = true;
            return;
        }

        Result = new AddGoalFundsRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id
        };
    }
}
