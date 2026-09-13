using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddIncomeSourceDialog : ContentDialog
{
    private readonly IncomeSource? _existingSource;

    public CreateIncomeSourceRequest? CreateResult { get; private set; }
    public UpdateIncomeSourceRequest? UpdateResult { get; private set; }

    public AddIncomeSourceDialog(List<Asset>? assets, IncomeSource? existingSource = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _existingSource = existingSource;

        var assetList = assets ?? new List<Asset>();
        DefaultAssetComboBox.ItemsSource = assetList;

        if (existingSource != null)
        {
            Title = "Edit Income Source";
            NameBox.Text = existingSource.Name ?? string.Empty;

            TypeRadioButtons.SelectedIndex = string.Equals(existingSource.Type, "PASSIVE", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            NatureRadioButtons.SelectedIndex = existingSource.IsFixed ? 0 : 1;

            ExpectedAmountBox.Value = existingSource.ExpectedAmount ?? 0;

            var matchFreqIndex = (existingSource.Frequency ?? string.Empty).ToUpperInvariant() switch
            {
                "DAILY" => 0,
                "WEEKLY" => 1,
                "MONTHLY" => 2,
                "YEARLY" => 3,
                _ => 4
            };
            FrequencyComboBox.SelectedIndex = matchFreqIndex;

            if (!string.IsNullOrEmpty(existingSource.DefaultAssetId))
            {
                var matchAsset = assetList.FirstOrDefault(a => a.Id == existingSource.DefaultAssetId);
                if (matchAsset != null)
                {
                    DefaultAssetComboBox.SelectedItem = matchAsset;
                }
            }

            DescriptionBox.Text = existingSource.Description ?? string.Empty;
            IconPathBox.Text = existingSource.IconUrl ?? string.Empty;
        }
        else
        {
            if (assetList.Count > 0)
            {
                DefaultAssetComboBox.SelectedIndex = 0;
            }
        }

        DefaultAssetComboBox.SelectionChanged += (_, _) => UpdateCurrencyDisplay();
        UpdateCurrencyDisplay();

        NumericInputHelper.Attach(ExpectedAmountBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += AddIncomeSourceDialog_PrimaryButtonClick;
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
        string? currency = null;
        if (DefaultAssetComboBox.SelectedItem is Asset selAsset && !string.IsNullOrWhiteSpace(selAsset.Currency))
        {
            currency = selAsset.Currency;
        }

        var symbol = "₹";
        try
        {
            if (!string.IsNullOrWhiteSpace(currency))
            {
                symbol = CountryCurrencyData.GetCurrencySymbol(currency);
            }
            else
            {
                var settings = App.GetService<ISettingsService>();
                if (!string.IsNullOrWhiteSpace(settings?.Settings?.CurrencySymbol))
                {
                    symbol = settings.Settings.CurrencySymbol;
                }
                else
                {
                    var auth = App.GetService<IAuthService>();
                    if (!string.IsNullOrWhiteSpace(auth?.CurrentUser?.Currency))
                    {
                        symbol = CountryCurrencyData.GetCurrencySymbol(auth.CurrentUser.Currency);
                    }
                }
            }
        }
        catch { }

        if (ExpectedAmountLabel != null)
        {
            ExpectedAmountLabel.Text = $"Expected Inflow ({symbol})";
        }
        if (ExpectedAmountBox != null)
        {
            ExpectedAmountBox.PlaceholderText = $"0.00 ({symbol})";
        }
    }

    private async void BrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");
            picker.FileTypeFilter.Add(".svg");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                IconPathBox.Text = file.Path;
            }
        }
        catch
        {
            // Fallback or ignore
        }
    }

    private void AddIncomeSourceDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            ShowError("Income Stream Name is required.");
            NameBox.Focus(FocusState.Programmatic);
            return;
        }

        var expectedAmount = ExpectedAmountBox.Value;
        if (!double.IsNaN(expectedAmount) && expectedAmount < 0)
        {
            args.Cancel = true;
            ShowError("Expected Inflow cannot be negative.");
            ExpectedAmountBox.Focus(FocusState.Programmatic);
            return;
        }

        var isPassive = (TypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "PASSIVE";
        var isFixed = (NatureRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "FIXED";
        var frequencyTag = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (frequencyTag == "FLEXIBLE") frequencyTag = null;

        var selectedAssetId = (DefaultAssetComboBox.SelectedItem as Asset)?.Id;
        var description = DescriptionBox.Text.Trim();
        var iconPath = IconPathBox.Text.Trim();

        if (_existingSource != null)
        {
            UpdateResult = new UpdateIncomeSourceRequest
            {
                Name = name,
                Type = isPassive ? "PASSIVE" : "ACTIVE",
                IsFixed = isFixed,
                ExpectedAmount = expectedAmount > 0 ? expectedAmount : null,
                Frequency = frequencyTag,
                DefaultAssetId = selectedAssetId,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                IconFilePath = !string.IsNullOrWhiteSpace(iconPath) && !iconPath.StartsWith("http") ? iconPath : null
            };
        }
        else
        {
            CreateResult = new CreateIncomeSourceRequest
            {
                Name = name,
                Type = isPassive ? "PASSIVE" : "ACTIVE",
                IsFixed = isFixed,
                ExpectedAmount = expectedAmount > 0 ? expectedAmount : null,
                Frequency = frequencyTag,
                DefaultAssetId = selectedAssetId,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                IconFilePath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath
            };
        }
    }
}
