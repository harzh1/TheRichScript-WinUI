using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddDebtDialog : ContentDialog
{
    public CreateDebtRequest? Result { get; private set; }
    public UpdateDebtRequest? UpdateResult { get; private set; }

    private readonly Debt? _existingDebt;
    private readonly List<Contact> _contacts;
    private string? _selectedContactId;
    private bool _isUpdatingFx;

    public AddDebtDialog(List<Asset> assets, List<Contact>? contacts = null, Debt? existingDebt = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        _contacts = contacts ?? new List<Contact>();
        _existingDebt = existingDebt;

        PersonBox.TextChanged += PersonBox_TextChanged;
        PersonBox.SuggestionChosen += PersonBox_SuggestionChosen;

        AssetComboBox.ItemsSource = assets;
        CurrencyComboBox.ItemsSource = CountryCurrencyData.AllCurrencies;

        if (_existingDebt != null)
        {
            Title = "Edit Debt / Loan";
            PrimaryButtonText = "Save Changes";

            PersonBox.Text = _existingDebt.PersonName;
            AmountNumberBox.Value = _existingDebt.Amount;
            _selectedContactId = _existingDebt.ContactId;
            NotesBox.Text = _existingDebt.Notes ?? string.Empty;

            if (_existingDebt.IsLent)
            {
                TypeRadioButtons.SelectedIndex = 0;
            }
            else
            {
                TypeRadioButtons.SelectedIndex = 1;
            }

            if (!string.IsNullOrWhiteSpace(_existingDebt.AssetId))
            {
                AssetComboBox.SelectedValue = _existingDebt.AssetId;
            }

            if (!string.IsNullOrWhiteSpace(_existingDebt.Currency))
            {
                CurrencyComboBox.SelectedValue = _existingDebt.Currency.ToUpperInvariant();
            }

            if (_existingDebt.DueDate.HasValue)
            {
                DatePicker.Date = new DateTimeOffset(_existingDebt.DueDate.Value);
            }
            else
            {
                DatePicker.Date = null;
            }
        }
        else
        {
            if (assets.Count > 0)
            {
                AssetComboBox.SelectedIndex = 0;
                var firstAsset = assets[0];
                CurrencyComboBox.SelectedValue = !string.IsNullOrWhiteSpace(firstAsset.Currency) ? firstAsset.Currency.ToUpperInvariant() : "INR";
            }
            else
            {
                CurrencyComboBox.SelectedValue = "INR";
            }

            DatePicker.Date = DateTimeOffset.Now.AddMonths(1);
        }

        AssetComboBox.SelectionChanged += (_, _) => UpdateCurrencyAndFx();
        CurrencyComboBox.SelectionChanged += (_, _) => UpdateCurrencyAndFx();
        TypeRadioButtons.SelectionChanged += (_, _) => UpdateCurrencyAndFx();
        AmountNumberBox.ValueChanged += AmountNumberBox_ValueChanged;
        AssetImpactNumberBox.ValueChanged += AssetImpactNumberBox_ValueChanged;
        ExchangeRateNumberBox.ValueChanged += ExchangeRateNumberBox_ValueChanged;
        UpdateCurrencyAndFx();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(AssetImpactNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(ExchangeRateNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += AddDebtDialog_PrimaryButtonClick;
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
                AssetImpactNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private void AssetImpactNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var impact = AssetImpactNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(impact) && impact > 0)
            {
                ExchangeRateNumberBox.Value = Math.Round(impact / amount, 6);
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
                AssetImpactNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private async void UpdateCurrencyAndFx()
    {
        var selectedDebtCurr = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedDebtCurr) && CurrencyComboBox.SelectedItem is CurrencyInfo ci)
        {
            selectedDebtCurr = ci.Code;
        }

        var isLent = (TypeRadioButtons?.SelectedItem as RadioButton)?.Tag?.ToString() == "LENT";

        if (AssetComboBox.SelectedItem is Asset selAsset)
        {
            var assetCurr = !string.IsNullOrWhiteSpace(selAsset.Currency) ? selAsset.Currency.ToUpperInvariant() : "INR";

            if (string.IsNullOrWhiteSpace(selectedDebtCurr))
            {
                selectedDebtCurr = assetCurr;
                CurrencyComboBox.SelectedValue = selectedDebtCurr;
            }

            var isCrossCurrency = !string.Equals(selectedDebtCurr, assetCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency)
            {
                var assetSymbol = CountryCurrencyData.GetCurrencySymbol(assetCurr);
                var debtSymbol = CountryCurrencyData.GetCurrencySymbol(selectedDebtCurr);

                ExchangeRatePanel.Visibility = Visibility.Visible;
                ExchangeRateLabel.Text = $"Exchange Rate ({assetCurr} per 1 {selectedDebtCurr})";
                AssetImpactLabel.Text = isLent
                    ? $"Account Amount Debited ({assetSymbol} {assetCurr})"
                    : $"Account Amount Credited ({assetSymbol} {assetCurr})";
                AssetImpactNumberBox.PlaceholderText = $"0.00 ({assetSymbol})";

                try
                {
                    var fxService = App.GetService<IFxRateService>();
                    if (fxService != null)
                    {
                        var liveRate = await fxService.GetExchangeRateAsync(selectedDebtCurr, assetCurr);
                        if (liveRate.HasValue && liveRate.Value > 0)
                        {
                            ExchangeRateNumberBox.PlaceholderText = $"Live: 1 {selectedDebtCurr} = {liveRate.Value:F4} {assetCurr}";
                            if (double.IsNaN(AssetImpactNumberBox.Value))
                            {
                                var amt = AmountNumberBox.Value;
                                if (!double.IsNaN(amt) && amt > 0)
                                {
                                    AssetImpactNumberBox.PlaceholderText = $"~ {assetSymbol}{amt * liveRate.Value:N2}";
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

        var curr = !string.IsNullOrWhiteSpace(selectedDebtCurr) ? selectedDebtCurr : "INR";
        var currentDebtSymbol = CountryCurrencyData.GetCurrencySymbol(curr);

        if (AmountLabel != null)
        {
            AmountLabel.Text = $"Amount ({currentDebtSymbol} {curr})";
        }
        if (AmountNumberBox != null)
        {
            AmountNumberBox.PlaceholderText = $"0.00 ({currentDebtSymbol})";
        }
    }

    private void PersonBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _selectedContactId = null;
            var query = sender.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(query))
            {
                sender.ItemsSource = null;
            }
            else
            {
                sender.ItemsSource = System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Select(
                        System.Linq.Enumerable.Where(_contacts, c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)),
                        c => c.Name));
            }
        }
    }

    private void PersonBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var chosenName = args.SelectedItem?.ToString();
        var match = System.Linq.Enumerable.FirstOrDefault(_contacts, c => string.Equals(c.Name, chosenName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            _selectedContactId = match.Id;
        }
    }

    private void AddDebtDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        var person = PersonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(person))
        {
            args.Cancel = true;
            ShowError("Person / Contact Name is required.");
            PersonBox.Focus(FocusState.Programmatic);
            return;
        }

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
            ShowError("Please select an Associated Account / Asset.");
            AssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var isLent = (TypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "LENT";
        var selectedContact = System.Linq.Enumerable.FirstOrDefault(_contacts, c => string.Equals(c.Name, person, StringComparison.OrdinalIgnoreCase));
        var contactId = _selectedContactId ?? selectedContact?.Id;

        var selectedCurrency = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurrency) && CurrencyComboBox.SelectedItem is CurrencyInfo currInfo)
        {
            selectedCurrency = currInfo.Code;
        }

        if (string.IsNullOrWhiteSpace(selectedCurrency))
        {
            args.Cancel = true;
            ShowError("Please select a debt currency.");
            CurrencyComboBox.Focus(FocusState.Programmatic);
            return;
        }

        double? exchangeRate = null;
        if (ExchangeRatePanel.Visibility == Visibility.Visible)
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
        }

        if (_existingDebt != null)
        {
            UpdateResult = new UpdateDebtRequest
            {
                PersonName = person,
                Amount = amount,
                AssetId = selectedAsset.Id,
                Currency = selectedCurrency,
                ExchangeRate = exchangeRate,
                Type = isLent ? "LENT" : "BORROWED",
                DueDate = DatePicker.Date?.DateTime,
                Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim(),
                ContactId = contactId
            };
        }
        else
        {
            Result = new CreateDebtRequest
            {
                PersonName = person,
                Amount = amount,
                AssetId = selectedAsset.Id,
                Currency = selectedCurrency,
                ExchangeRate = exchangeRate,
                Type = isLent ? "LENT" : "BORROWED",
                DueDate = DatePicker.Date?.DateTime,
                Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim(),
                ContactId = contactId
            };
        }
    }
}
