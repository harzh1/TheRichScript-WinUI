using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class DeleteGoalDialog : ContentDialog
{
    private readonly Goal _goal;
    private readonly bool _hasBalance;

    public string? SelectedRefundAssetId { get; private set; }

    public DeleteGoalDialog(Goal goal, List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _goal = goal;

        var symbol = !string.IsNullOrWhiteSpace(goal.Currency) ? goal.CurrencySymbol : "₹";
        if (string.IsNullOrWhiteSpace(goal.Currency))
        {
            try
            {
                var settings = App.GetService<ISettingsService>();
                if (settings?.Settings?.CurrencySymbol != null) symbol = settings.Settings.CurrencySymbol;
            }
            catch { }
        }

        _hasBalance = goal.CurrentAmount > 0;

        if (_hasBalance)
        {
            Title = "Delete Goal & Refund Balance";
            PrimaryButtonText = "Refund & Delete";
            WarningInfoBar.Severity = InfoBarSeverity.Warning;
            WarningInfoBar.Message = $"Deleting '{goal.Name}' will refund the saved balance and permanently delete this goal.";

            GoalSummaryText.Text = $"Goal: {goal.Name}\nTarget: {symbol}{goal.TargetAmount:N2}";
            RefundPromptText.Text = $"This goal has a saved balance of {symbol}{goal.CurrentAmount:N2}. Where should we transfer and refund this money?";
            RefundSection.Visibility = Visibility.Visible;

            AssetComboBox.ItemsSource = assets;
            if (assets.Count > 0)
            {
                var defaultAsset = assets.FirstOrDefault(a => a.Id == goal.SourceAssetId || a.Id == goal.TargetAssetId) ?? assets[0];
                AssetComboBox.SelectedItem = defaultAsset;
            }
        }
        else
        {
            Title = "Delete Financial Goal";
            PrimaryButtonText = "Delete Goal";
            WarningInfoBar.Severity = InfoBarSeverity.Informational;
            WarningInfoBar.Message = $"Are you sure you want to delete '{goal.Name}'? This action cannot be undone.";

            GoalSummaryText.Text = $"Goal: {goal.Name}\nSaved Balance: {symbol}0.00 (No refund required)";
            RefundSection.Visibility = Visibility.Collapsed;
        }

        PrimaryButtonClick += DeleteGoalDialog_PrimaryButtonClick;
    }

    private void DeleteGoalDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_hasBalance)
        {
            if (AssetComboBox.SelectedItem is not Asset selectedAsset)
            {
                ValidationErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            SelectedRefundAssetId = selectedAsset.Id;
        }
        else
        {
            SelectedRefundAssetId = null;
        }
    }
}
