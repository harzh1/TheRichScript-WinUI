using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddIncomeSourceDialog : ContentDialog
{
    private readonly IncomeSource? _existingSource;

    public CreateIncomeSourceRequest? CreateResult { get; private set; }
    public UpdateIncomeSourceRequest? UpdateResult { get; private set; }

    public AddIncomeSourceDialog(List<Asset> assets, IncomeSource? existingSource = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _existingSource = existingSource;

        DefaultAssetComboBox.ItemsSource = assets;

        if (existingSource != null)
        {
            Title = "Edit Income Source";
            NameBox.Text = existingSource.Name;

            TypeRadioButtons.SelectedIndex = existingSource.Type == "PASSIVE" ? 1 : 0;
            NatureRadioButtons.SelectedIndex = existingSource.IsFixed ? 0 : 1;

            ExpectedAmountBox.Value = existingSource.ExpectedAmount ?? 0;

            var matchFreqIndex = existingSource.Frequency switch
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
                var matchAsset = assets.FirstOrDefault(a => a.Id == existingSource.DefaultAssetId);
                if (matchAsset != null)
                {
                    DefaultAssetComboBox.SelectedItem = matchAsset;
                }
            }

            DescriptionBox.Text = existingSource.Description ?? "";
            IconPathBox.Text = existingSource.IconUrl ?? "";
        }
        else
        {
            if (assets.Count > 0)
            {
                DefaultAssetComboBox.SelectedIndex = 0;
            }
        }

        PrimaryButtonClick += AddIncomeSourceDialog_PrimaryButtonClick;
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
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            return;
        }

        var isPassive = (TypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "PASSIVE";
        var isFixed = (NatureRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "FIXED";
        var expectedAmount = ExpectedAmountBox.Value;
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
