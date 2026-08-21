using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddAssetDialog : ContentDialog
{
    private readonly List<string> _availableTypes;

    public CreateAssetRequest? Result { get; private set; }

    public AddAssetDialog(List<string> existingTypes, Asset? assetToEdit = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _availableTypes = existingTypes ?? new List<string> { "BANK", "INVESTMENT", "CRYPTO", "REAL ESTATE", "CASH", "GOLD" };

        if (assetToEdit != null)
        {
            Title = "Edit Asset";
            NameBox.Text = assetToEdit.Name;
            TypeBox.Text = assetToEdit.Type;
            ValueNumberBox.Value = assetToEdit.Value;
            if (assetToEdit.Quantity.HasValue) QuantityNumberBox.Value = assetToEdit.Quantity.Value;
        }

        PrimaryButtonClick += AddAssetDialog_PrimaryButtonClick;
    }

    private void TypeBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text.Trim().ToUpperInvariant();
            sender.ItemsSource = string.IsNullOrWhiteSpace(query)
                ? _availableTypes
                : _availableTypes.Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    private void AddAssetDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameBox.Text.Trim();
        var type = TypeBox.Text.Trim().ToUpperInvariant();
        var value = ValueNumberBox.Value;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type) || double.IsNaN(value))
        {
            args.Cancel = true;
            return;
        }

        var qty = QuantityNumberBox.Value;

        Result = new CreateAssetRequest
        {
            Name = name,
            Type = type,
            Value = value,
            Quantity = double.IsNaN(qty) ? null : qty
        };
    }
}
