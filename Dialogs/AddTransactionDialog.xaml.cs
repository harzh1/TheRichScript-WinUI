using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddTransactionDialog : ContentDialog
{
    private readonly List<Asset> _assets;
    private readonly List<string> _availableCategories;
    private readonly List<IncomeSource> _availableIncomeSources;

    private const string CreateNewCategoryOption = "+ Create New Category...";
    private const string CreateNewIncomeSourceOption = "+ Add New Income Source...";

    public CreateTransactionRequest? Result { get; private set; }

    public AddTransactionDialog(List<Asset> assets, List<string> categories, List<IncomeSource>? incomeSources = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _assets = assets ?? new List<Asset>();
        _availableCategories = categories ?? new List<string>();
        _availableIncomeSources = incomeSources ?? new List<IncomeSource>();

        AssetComboBox.ItemsSource = _assets;
        if (_assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        // Setup Categories
        var catList = new List<string>(_availableCategories);
        if (!catList.Contains(CreateNewCategoryOption))
        {
            catList.Add(CreateNewCategoryOption);
        }

        CategoryComboBox.ItemsSource = catList;
        if (catList.Count > 0)
        {
            CategoryComboBox.SelectedIndex = 0;
        }

        // Setup Income Sources
        var sourceDisplayList = new List<string>();
        foreach (var s in _availableIncomeSources)
        {
            sourceDisplayList.Add(s.Name);
        }
        if (!sourceDisplayList.Contains("Others", StringComparer.OrdinalIgnoreCase))
        {
            sourceDisplayList.Add("Others");
        }
        sourceDisplayList.Add(CreateNewIncomeSourceOption);

        IncomeSourceComboBox.ItemsSource = sourceDisplayList;
        if (sourceDisplayList.Count > 0)
        {
            IncomeSourceComboBox.SelectedIndex = 0;
        }

        DatePicker.Date = DateTimeOffset.Now;

        PrimaryButtonClick += AddTransactionDialog_PrimaryButtonClick;
    }

    private void TypeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isIncome = (TypeRadioButtons?.SelectedItem as RadioButton)?.Tag?.ToString() == "INCOME";
        if (CategoryPanel != null)
        {
            CategoryPanel.Visibility = isIncome ? Visibility.Collapsed : Visibility.Visible;
        }
        if (IncomeSourcePanel != null)
        {
            IncomeSourcePanel.Visibility = isIncome ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is string selected)
        {
            if (selected == CreateNewCategoryOption)
            {
                NewCategoryTextBox.Visibility = Visibility.Visible;
                NewCategoryTextBox.Focus(FocusState.Programmatic);
            }
            else
            {
                NewCategoryTextBox.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void IncomeSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IncomeSourceComboBox.SelectedItem is string selected)
        {
            if (selected == CreateNewIncomeSourceOption)
            {
                NewIncomeSourceTextBox.Visibility = Visibility.Visible;
                NewIncomeSourceTextBox.Focus(FocusState.Programmatic);
            }
            else
            {
                NewIncomeSourceTextBox.Visibility = Visibility.Collapsed;

                // Auto-select associated default asset if configured
                var match = _availableIncomeSources.FirstOrDefault(s => string.Equals(s.Name, selected, StringComparison.OrdinalIgnoreCase));
                if (match != null && !string.IsNullOrEmpty(match.DefaultAssetId))
                {
                    var matchAsset = _assets.FirstOrDefault(a => a.Id == match.DefaultAssetId);
                    if (matchAsset != null)
                    {
                        AssetComboBox.SelectedItem = matchAsset;
                    }
                }
            }
        }
    }

    private async void BrowseReceipt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ReceiptPathBox.Text = file.Path;
            }
        }
        catch
        {
            // Fallback or ignore
        }
    }

    private void AddTransactionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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

        var isIncome = (TypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "INCOME";

        string category = "INCOME";
        string? incomeSourceId = null;
        string? incomeSourceName = null;

        if (isIncome)
        {
            if (IncomeSourceComboBox.SelectedItem is string selectedSource)
            {
                if (selectedSource == CreateNewIncomeSourceOption)
                {
                    incomeSourceName = NewIncomeSourceTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(incomeSourceName))
                    {
                        incomeSourceName = "Others";
                    }
                }
                else
                {
                    incomeSourceName = selectedSource;
                    var match = _availableIncomeSources.FirstOrDefault(s => string.Equals(s.Name, selectedSource, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        incomeSourceId = match.Id;
                    }
                }
            }
            else
            {
                incomeSourceName = "Others";
            }
        }
        else
        {
            if (CategoryComboBox.SelectedItem is string selectedCat)
            {
                if (selectedCat == CreateNewCategoryOption)
                {
                    category = NewCategoryTextBox.Text.Trim();
                }
                else
                {
                    category = selectedCat.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                args.Cancel = true;
                return;
            }
        }

        Result = new CreateTransactionRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id,
            Category = category.ToUpperInvariant(),
            IncomeSourceId = incomeSourceId,
            IncomeSourceName = incomeSourceName,
            Description = DescriptionBox.Text?.Trim() ?? string.Empty,
            Type = isIncome ? "INCOME" : "EXPENSE",
            Date = DatePicker.Date?.DateTime ?? DateTime.Now,
            ReceiptFilePath = string.IsNullOrWhiteSpace(ReceiptPathBox.Text) ? null : ReceiptPathBox.Text
        };
    }
}
