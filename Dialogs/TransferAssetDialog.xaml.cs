using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class TransferAssetDialog : ContentDialog
{
    private bool _isUpdatingFx;
    public TransferAssetRequest? Result { get; private set; }

    public TransferAssetDialog(List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        FromAssetComboBox.ItemsSource = assets;
        ToAssetComboBox.ItemsSource = assets;

        if (assets.Count > 0)
        {
            FromAssetComboBox.SelectedIndex = 0;
            if (assets.Count > 1)
            {
                ToAssetComboBox.SelectedIndex = 1;
            }
        }

        DatePicker.Date = DateTimeOffset.Now;

        FromAssetComboBox.SelectionChanged += (_, _) => UpdateFxVisibility();
        ToAssetComboBox.SelectionChanged += (_, _) => UpdateFxVisibility();
        AmountNumberBox.ValueChanged += AmountNumberBox_ValueChanged;
        DestAmountNumberBox.ValueChanged += DestAmountNumberBox_ValueChanged;
        ExchangeRateNumberBox.ValueChanged += ExchangeRateNumberBox_ValueChanged;
        UpdateFxVisibility();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(DestAmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(ExchangeRateNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += TransferAssetDialog_PrimaryButtonClick;
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
                DestAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private void DestAmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var dest = DestAmountNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(dest) && dest > 0)
            {
                ExchangeRateNumberBox.Value = Math.Round(dest / amount, 6);
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
                DestAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private async void UpdateFxVisibility()
    {
        if (FromAssetComboBox.SelectedItem is Asset fromAsset && ToAssetComboBox.SelectedItem is Asset toAsset)
        {
            var fromCurr = !string.IsNullOrWhiteSpace(fromAsset.Currency) ? fromAsset.Currency.ToUpperInvariant() : "INR";
            var toCurr = !string.IsNullOrWhiteSpace(toAsset.Currency) ? toAsset.Currency.ToUpperInvariant() : "INR";
            var fromSymbol = CountryCurrencyData.GetCurrencySymbol(fromCurr);
            var toSymbol = CountryCurrencyData.GetCurrencySymbol(toCurr);

            AmountLabel.Text = $"Transfer Amount ({fromSymbol} {fromCurr})";
            AmountNumberBox.PlaceholderText = $"0.00 ({fromSymbol})";

            var isCrossCurrency = !string.Equals(fromCurr, toCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency)
            {
                FxPanel.Visibility = Visibility.Visible;
                FxExplanationText.Text = $"Cross-currency transfer ({fromCurr} → {toCurr}). Custom credited amount or exchange rate is optional; backend automatically applies live rates if omitted.";
                DestAmountLabel.Text = $"Destination Amount Credited ({toSymbol} {toCurr})";
                DestAmountNumberBox.PlaceholderText = $"0.00 ({toSymbol})";
                ExchangeRateLabel.Text = $"Exchange Rate ({toCurr} per 1 {fromCurr})";

                try
                {
                    var fxService = App.GetService<IFxRateService>();
                    if (fxService != null)
                    {
                        var liveRate = await fxService.GetExchangeRateAsync(fromCurr, toCurr);
                        if (liveRate.HasValue && liveRate.Value > 0)
                        {
                            ExchangeRateNumberBox.PlaceholderText = $"Live: 1 {fromCurr} = {liveRate.Value:F4} {toCurr}";
                            if (double.IsNaN(DestAmountNumberBox.Value))
                            {
                                var amt = AmountNumberBox.Value;
                                if (!double.IsNaN(amt) && amt > 0)
                                {
                                    DestAmountNumberBox.PlaceholderText = $"~ {toSymbol}{amt * liveRate.Value:N2}";
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                FxPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void TransferAssetDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        if (FromAssetComboBox.SelectedItem is not Asset fromAsset)
        {
            args.Cancel = true;
            ShowError("Please select a Source Account (From).");
            FromAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        if (ToAssetComboBox.SelectedItem is not Asset toAsset)
        {
            args.Cancel = true;
            ShowError("Please select a Destination Account (To).");
            ToAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        if (fromAsset.Id == toAsset.Id)
        {
            args.Cancel = true;
            ShowError("Source and Destination accounts cannot be the same.");
            ToAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            ShowError("Please enter a transfer amount greater than 0.");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        if (fromAsset.Value < amount)
        {
            args.Cancel = true;
            ShowError($"Insufficient funds in {fromAsset.Name}. Available: {fromAsset.Currency} {fromAsset.Value:N2}");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        double? destAmount = null;
        double? exchangeRate = null;

        var isCrossCurrency = !string.Equals(fromAsset.Currency, toAsset.Currency, StringComparison.OrdinalIgnoreCase);
        if (isCrossCurrency)
        {
            var da = DestAmountNumberBox.Value;
            if (!double.IsNaN(da))
            {
                if (da <= 0)
                {
                    args.Cancel = true;
                    ShowError("Destination credited amount must be greater than 0.");
                    DestAmountNumberBox.Focus(FocusState.Programmatic);
                    return;
                }
                destAmount = da;
            }

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

        Result = new TransferAssetRequest
        {
            FromAssetId = fromAsset.Id,
            ToAssetId = toAsset.Id,
            Amount = amount,
            DestinationAmount = destAmount,
            ExchangeRate = exchangeRate,
            Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? $"Transfer to {toAsset.Name}" : DescriptionBox.Text.Trim(),
            Date = DatePicker.Date?.DateTime ?? DateTime.Now
        };
    }
}
