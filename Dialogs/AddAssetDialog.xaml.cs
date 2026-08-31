using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddAssetDialog : ContentDialog
{
    private const string CreateNewTypeOption = "+ Add New Asset Type...";

    private static readonly string[] DefaultCommonTypes =
    [
        "Bank",
        "Metals",
        "Mutual Fund",
        "Crypto",
        "Cash",
        "Real Estate"
    ];

    public CreateAssetRequest? Result { get; private set; }

    public AddAssetDialog(List<string>? existingTypes, Asset? assetToEdit = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        // Build list starting with common types requested by user
        var typeList = new List<string>(DefaultCommonTypes);

        // Add any existing types from the portfolio that aren't already present
        if (existingTypes != null)
        {
            foreach (var t in existingTypes)
            {
                if (!string.IsNullOrWhiteSpace(t) && !typeList.Any(x => x.Equals(t, StringComparison.OrdinalIgnoreCase)))
                {
                    typeList.Add(t);
                }
            }
        }

        // Add "+ Add New Asset Type..." at the bottom
        typeList.Add(CreateNewTypeOption);

        TypeComboBox.ItemsSource = typeList;

        if (assetToEdit != null)
        {
            Title = "Edit Asset";
            NameBox.Text = assetToEdit.Name;
            ValueNumberBox.Value = assetToEdit.Value;

            if (assetToEdit.Quantity.HasValue)
            {
                QuantityNumberBox.Value = assetToEdit.Quantity.Value;
            }
            else
            {
                QuantityNumberBox.Value = double.NaN;
            }

            // Find matching item in dropdown or insert custom
            var match = typeList.FirstOrDefault(t => t.Equals(assetToEdit.Type, StringComparison.OrdinalIgnoreCase));
            if (match != null && match != CreateNewTypeOption)
            {
                TypeComboBox.SelectedItem = match;
            }
            else
            {
                typeList.Insert(typeList.Count - 1, assetToEdit.Type);
                TypeComboBox.ItemsSource = null;
                TypeComboBox.ItemsSource = typeList;
                TypeComboBox.SelectedItem = assetToEdit.Type;
            }
        }
        else
        {
            // Default for new asset: Bank is pre-selected, Value is 0, Quantity is empty (NaN)
            TypeComboBox.SelectedIndex = 0;
            ValueNumberBox.Value = 0;
            QuantityNumberBox.Value = double.NaN;
        }

        PrimaryButtonClick += AddAssetDialog_PrimaryButtonClick;
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeComboBox.SelectedItem is string selected)
        {
            if (selected == CreateNewTypeOption)
            {
                NewTypeTextBox.Visibility = Visibility.Visible;
                NewTypeTextBox.Focus(FocusState.Programmatic);
            }
            else
            {
                NewTypeTextBox.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void AddAssetDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameBox.Text.Trim();
        var value = ValueNumberBox.Value;

        string type;
        if (TypeComboBox.SelectedItem is string selected && selected == CreateNewTypeOption)
        {
            type = NewTypeTextBox.Text.Trim();
        }
        else
        {
            type = (TypeComboBox.SelectedItem as string)?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type) || double.IsNaN(value))
        {
            args.Cancel = true;
            return;
        }

        var qty = QuantityNumberBox.Value;

        Result = new CreateAssetRequest
        {
            Name = name,
            Type = type.ToUpperInvariant(),
            Value = value,
            Quantity = double.IsNaN(qty) ? null : qty
        };
    }
}
