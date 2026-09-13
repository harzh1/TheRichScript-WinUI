using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

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

        CurrencyComboBox.ItemsSource = CountryCurrencyData.AllCurrencies;

        if (assetToEdit != null)
        {
            Title = "Edit Asset";
            NameBox.Text = assetToEdit.Name;
            ValueNumberBox.Value = assetToEdit.Value;

            if (!string.IsNullOrWhiteSpace(assetToEdit.Currency))
            {
                CurrencyComboBox.SelectedValue = assetToEdit.Currency.ToUpperInvariant();
            }
            else
            {
                SetDefaultCurrency();
            }

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
            SetDefaultCurrency();
        }

        CurrencyComboBox.SelectionChanged += (_, _) => UpdateCurrencyDisplay();
        UpdateCurrencyDisplay();

        NumericInputHelper.Attach(ValueNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(QuantityNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += AddAssetDialog_PrimaryButtonClick;
    }

    private void ShowError(string message)
    {
        DialogErrorBar.Message = message;
        DialogErrorBar.IsOpen = true;
    }

    private void HideError()
    {
        DialogErrorBar.IsOpen = false;
        DialogErrorBar.Message = string.Empty;
    }

    private void UpdateCurrencyDisplay()
    {
        var selectedCurr = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurr) && CurrencyComboBox.SelectedItem is CurrencyInfo ci)
        {
            selectedCurr = ci.Code;
        }

        var symbol = CountryCurrencyData.GetCurrencySymbol(selectedCurr);
        if (ValueLabel != null)
        {
            ValueLabel.Text = $"Current Value / Balance ({symbol})";
        }
        if (ValueNumberBox != null)
        {
            ValueNumberBox.PlaceholderText = $"0.00 ({symbol})";
        }
    }

    private void SetDefaultCurrency()
    {
        var defaultCurr = "INR";
        try
        {
            var authService = App.GetService<TheRichScript_WinUI.Services.IAuthService>();
            if (!string.IsNullOrWhiteSpace(authService?.CurrentUser?.Currency))
            {
                defaultCurr = authService.CurrentUser.Currency.ToUpperInvariant();
            }
        }
        catch { }
        CurrencyComboBox.SelectedValue = defaultCurr;
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
        HideError();

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            ShowError("Asset / Account Name is required.");
            NameBox.Focus(FocusState.Programmatic);
            return;
        }

        string type;
        if (TypeComboBox.SelectedItem is string selected && selected == CreateNewTypeOption)
        {
            type = NewTypeTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(type))
            {
                args.Cancel = true;
                ShowError("Please enter the new asset type.");
                NewTypeTextBox.Focus(FocusState.Programmatic);
                return;
            }
        }
        else
        {
            type = (TypeComboBox.SelectedItem as string)?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            args.Cancel = true;
            ShowError("Please select an asset type.");
            TypeComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var value = ValueNumberBox.Value;
        if (double.IsNaN(value) || value < 0)
        {
            args.Cancel = true;
            ShowError("Please enter a valid balance / value (0 or greater).");
            ValueNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        var qty = QuantityNumberBox.Value;
        if (!double.IsNaN(qty) && qty < 0)
        {
            args.Cancel = true;
            ShowError("Quantity cannot be negative.");
            QuantityNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        var selectedCurrency = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurrency) && CurrencyComboBox.SelectedItem is CurrencyInfo ci)
        {
            selectedCurrency = ci.Code;
        }

        if (string.IsNullOrWhiteSpace(selectedCurrency))
        {
            args.Cancel = true;
            ShowError("Please select a currency.");
            CurrencyComboBox.Focus(FocusState.Programmatic);
            return;
        }

        Result = new CreateAssetRequest
        {
            Name = name,
            Type = type.ToUpperInvariant(),
            Value = value,
            Quantity = double.IsNaN(qty) ? null : qty,
            Currency = selectedCurrency
        };
    }
}
