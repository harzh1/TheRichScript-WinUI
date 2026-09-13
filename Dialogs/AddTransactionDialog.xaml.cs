using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddTransactionDialog : ContentDialog
{
    private readonly List<Asset> _assets;
    private readonly List<string> _availableCategories;
    private readonly List<IncomeSource> _availableIncomeSources;
    private readonly Transaction? _existingTransaction;
    private bool _isUpdatingFx;

    private const string CreateNewCategoryOption = "+ Create New Category...";
    private const string CreateNewIncomeSourceOption = "+ Add New Income Source...";

    public CreateTransactionRequest? Result { get; private set; }
    public UpdateTransactionRequest? UpdateResult { get; private set; }
    public bool IsEditMode => _existingTransaction != null;

    public AddTransactionDialog(
        List<Asset> assets,
        List<string> availableCategories,
        List<IncomeSource> availableIncomeSources,
        Transaction? existingTransaction = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        _assets = assets;
        _availableCategories = availableCategories;
        _availableIncomeSources = availableIncomeSources;
        _existingTransaction = existingTransaction;

        AssetComboBox.ItemsSource = _assets;
        CurrencyComboBox.ItemsSource = CountryCurrencyData.AllCurrencies;

        // Setup Categories
        var catList = new List<string>(_availableCategories);
        if (!catList.Contains("Others", StringComparer.OrdinalIgnoreCase))
        {
            catList.Add("Others");
        }
        catList.Add(CreateNewCategoryOption);
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

        if (existingTransaction != null)
        {
            Title = "Edit Transaction";
            PrimaryButtonText = "Save Changes";

            // Pre-select Type
            if (existingTransaction.IsIncome)
            {
                TypeRadioButtons.SelectedIndex = 1;
                CategoryPanel.Visibility = Visibility.Collapsed;
                IncomeSourcePanel.Visibility = Visibility.Visible;
            }
            else
            {
                TypeRadioButtons.SelectedIndex = 0;
                CategoryPanel.Visibility = Visibility.Visible;
                IncomeSourcePanel.Visibility = Visibility.Collapsed;
            }

            // Pre-select Amount
            AmountNumberBox.Value = existingTransaction.Amount;

            // Pre-select Asset
            var matchAsset = _assets.FirstOrDefault(a => a.Id == existingTransaction.AssetId);
            if (matchAsset != null)
            {
                AssetComboBox.SelectedItem = matchAsset;
            }

            // Pre-select Currency & Exchange Rate
            if (!string.IsNullOrWhiteSpace(existingTransaction.Currency))
            {
                CurrencyComboBox.SelectedValue = existingTransaction.Currency.ToUpperInvariant();
            }
            else if (matchAsset != null && !string.IsNullOrWhiteSpace(matchAsset.Currency))
            {
                CurrencyComboBox.SelectedValue = matchAsset.Currency.ToUpperInvariant();
            }

            if (existingTransaction.ExchangeRate.HasValue)
            {
                ExchangeRateNumberBox.Value = existingTransaction.ExchangeRate.Value;
            }

            if (existingTransaction.TargetAmount.HasValue)
            {
                TargetAmountNumberBox.Value = existingTransaction.TargetAmount.Value;
            }

            // Pre-select Category
            if (!string.IsNullOrWhiteSpace(existingTransaction.Category))
            {
                if (!catList.Contains(existingTransaction.Category, StringComparer.OrdinalIgnoreCase))
                {
                    catList.Insert(0, existingTransaction.Category);
                }
                CategoryComboBox.SelectedItem = catList.FirstOrDefault(c => string.Equals(c, existingTransaction.Category, StringComparison.OrdinalIgnoreCase));
            }

            // Pre-select Income Source
            if (!string.IsNullOrWhiteSpace(existingTransaction.IncomeSourceName))
            {
                if (!sourceDisplayList.Contains(existingTransaction.IncomeSourceName, StringComparer.OrdinalIgnoreCase))
                {
                    sourceDisplayList.Insert(0, existingTransaction.IncomeSourceName);
                }
                IncomeSourceComboBox.SelectedItem = sourceDisplayList.FirstOrDefault(s => string.Equals(s, existingTransaction.IncomeSourceName, StringComparison.OrdinalIgnoreCase));
            }

            DescriptionBox.Text = existingTransaction.Description ?? string.Empty;
            DatePicker.Date = existingTransaction.Date;

            if (!string.IsNullOrWhiteSpace(existingTransaction.ReceiptImageUrl))
            {
                ReceiptPathBox.PlaceholderText = "Existing receipt attached";
            }
        }
        else
        {
            if (_assets.Count > 0)
            {
                AssetComboBox.SelectedIndex = 0;
                var firstAsset = _assets[0];
                CurrencyComboBox.SelectedValue = !string.IsNullOrWhiteSpace(firstAsset.Currency) ? firstAsset.Currency.ToUpperInvariant() : "INR";
            }
        }

        AssetComboBox.SelectionChanged += (_, _) => UpdateCurrencyAndFx();
        CurrencyComboBox.SelectionChanged += (_, _) => UpdateCurrencyAndFx();
        AmountNumberBox.ValueChanged += AmountNumberBox_ValueChanged;
        ExchangeRateNumberBox.ValueChanged += ExchangeRateNumberBox_ValueChanged;
        TargetAmountNumberBox.ValueChanged += TargetAmountNumberBox_ValueChanged;
        UpdateCurrencyAndFx();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(TargetAmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(ExchangeRateNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += AddTransactionDialog_PrimaryButtonClick;
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

    private void AmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var rate = ExchangeRateNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(rate) && rate > 0)
            {
                TargetAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private void ExchangeRateNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var rate = ExchangeRateNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(rate) && rate > 0)
            {
                TargetAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private void TargetAmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var target = TargetAmountNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(target) && target > 0)
            {
                ExchangeRateNumberBox.Value = Math.Round(target / amount, 6);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private async void UpdateCurrencyAndFx()
    {
        var selectedTxCurr = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedTxCurr) && CurrencyComboBox.SelectedItem is CurrencyInfo ci)
        {
            selectedTxCurr = ci.Code;
        }

        var isIncome = (TypeRadioButtons?.SelectedItem as RadioButton)?.Tag?.ToString() == "INCOME";

        if (AssetComboBox.SelectedItem is Asset selAsset)
        {
            var assetCurr = !string.IsNullOrWhiteSpace(selAsset.Currency) ? selAsset.Currency.ToUpperInvariant() : "INR";

            if (string.IsNullOrWhiteSpace(selectedTxCurr))
            {
                selectedTxCurr = assetCurr;
                CurrencyComboBox.SelectedValue = selectedTxCurr;
            }

            var isCrossCurrency = !string.Equals(selectedTxCurr, assetCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency)
            {
                var assetSymbol = CountryCurrencyData.GetCurrencySymbol(assetCurr);
                var txSymbol = CountryCurrencyData.GetCurrencySymbol(selectedTxCurr);

                ExchangeRatePanel.Visibility = Visibility.Visible;
                ExchangeRateLabel.Text = $"Exchange Rate ({assetCurr} per 1 {selectedTxCurr})";
                TargetAmountLabel.Text = isIncome
                    ? $"Account Amount Credited ({assetSymbol} {assetCurr})"
                    : $"Account Amount Debited ({assetSymbol} {assetCurr})";
                TargetAmountNumberBox.PlaceholderText = $"0.00 ({assetSymbol})";

                try
                {
                    var fxService = App.GetService<IFxRateService>();
                    if (fxService != null)
                    {
                        var liveRate = await fxService.GetExchangeRateAsync(selectedTxCurr, assetCurr);
                        if (liveRate.HasValue && liveRate.Value > 0)
                        {
                            ExchangeRateNumberBox.PlaceholderText = $"Live: 1 {selectedTxCurr} = {liveRate.Value:F4} {assetCurr}";
                            if (double.IsNaN(ExchangeRateNumberBox.Value) && double.IsNaN(TargetAmountNumberBox.Value))
                            {
                                var amt = AmountNumberBox.Value;
                                if (!double.IsNaN(amt) && amt > 0)
                                {
                                    TargetAmountNumberBox.PlaceholderText = $"~ {assetSymbol}{amt * liveRate.Value:N2}";
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                ExchangeRatePanel.Visibility = Visibility.Collapsed;
            }
        }

        var curr = !string.IsNullOrWhiteSpace(selectedTxCurr) ? selectedTxCurr : "INR";
        var currentTxSymbol = CountryCurrencyData.GetCurrencySymbol(curr);

        if (AmountLabel != null)
        {
            AmountLabel.Text = $"Amount ({currentTxSymbol} {curr})";
        }
        if (AmountNumberBox != null)
        {
            AmountNumberBox.PlaceholderText = $"0.00 ({currentTxSymbol})";
        }
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
        HideError();

        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            ShowError("Please enter a valid amount greater than 0.");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            ShowError("Please select an Account / Asset.");
            AssetComboBox.Focus(FocusState.Programmatic);
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
                        args.Cancel = true;
                        ShowError("Please enter an income source name.");
                        NewIncomeSourceTextBox.Focus(FocusState.Programmatic);
                        return;
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
                    if (string.IsNullOrWhiteSpace(category))
                    {
                        args.Cancel = true;
                        ShowError("Please enter a new category name.");
                        NewCategoryTextBox.Focus(FocusState.Programmatic);
                        return;
                    }
                }
                else
                {
                    category = selectedCat.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                args.Cancel = true;
                ShowError("Please select or enter a category.");
                CategoryComboBox.Focus(FocusState.Programmatic);
                return;
            }
        }

        var selectedCurrency = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurrency) && CurrencyComboBox.SelectedItem is CurrencyInfo currInfo)
        {
            selectedCurrency = currInfo.Code;
        }

        double? exchangeRate = null;
        double? targetAmount = null;

        var isCrossCurrency = !string.Equals(selectedCurrency, selectedAsset.Currency, StringComparison.OrdinalIgnoreCase);
        if (isCrossCurrency)
        {
            var er = ExchangeRateNumberBox.Value;
            if (!double.IsNaN(er))
            {
                if (er <= 0)
                {
                    args.Cancel = true;
                    ShowError("Exchange rate must be greater than 0.");
                    ExchangeRateNumberBox.Focus(FocusState.Programmatic);
                    return;
                }
                exchangeRate = er;
            }

            var ta = TargetAmountNumberBox.Value;
            if (!double.IsNaN(ta))
            {
                if (ta <= 0)
                {
                    args.Cancel = true;
                    ShowError("Converted account amount must be greater than 0.");
                    TargetAmountNumberBox.Focus(FocusState.Programmatic);
                    return;
                }
                targetAmount = ta;
            }
        }

        Result = new CreateTransactionRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id,
            Currency = selectedCurrency,
            ExchangeRate = exchangeRate,
            TargetAmount = targetAmount,
            Category = category.ToUpperInvariant(),
            IncomeSourceId = incomeSourceId,
            IncomeSourceName = incomeSourceName,
            Description = DescriptionBox.Text?.Trim() ?? string.Empty,
            Type = isIncome ? "INCOME" : "EXPENSE",
            Date = DatePicker.Date?.DateTime ?? DateTime.Now,
            ReceiptFilePath = string.IsNullOrWhiteSpace(ReceiptPathBox.Text) ? null : ReceiptPathBox.Text
        };

        if (IsEditMode)
        {
            UpdateResult = new UpdateTransactionRequest
            {
                Amount = amount,
                AssetId = selectedAsset.Id,
                Currency = selectedCurrency,
                ExchangeRate = exchangeRate,
                TargetAmount = targetAmount,
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
}
