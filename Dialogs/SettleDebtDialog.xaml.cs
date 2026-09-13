using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class SettleDebtDialog : ContentDialog
{
    private readonly Debt _debt;
    private bool _isUpdatingFx;
    public SettleDebtRequest? Result { get; private set; }

    public SettleDebtDialog(Debt debt, List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _debt = debt;

        var symbol = debt.CurrencySymbol;
        DebtInfoText.Text = $"Person: {debt.PersonName}\nRemaining Balance: {symbol} {debt.RemainingAmount:N2} ({(debt.IsLent ? "Receivable from them" : "Payable to them")})";
        AssetLabel.Text = debt.IsLent ? "Account Receiving Payment" : "Account Paying Debt";
        AmountLabel.Text = $"Settlement Amount ({symbol} {debt.Currency ?? "INR"})";
        AmountNumberBox.PlaceholderText = $"0.00 ({symbol})";

        AmountNumberBox.Maximum = debt.RemainingAmount;
        AmountNumberBox.Value = debt.RemainingAmount;

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        AssetComboBox.SelectionChanged += (_, _) => UpdateFxVisibility();
        AmountNumberBox.ValueChanged += AmountNumberBox_ValueChanged;
        SettleAssetAmountNumberBox.ValueChanged += SettleAssetAmountNumberBox_ValueChanged;
        ExchangeRateNumberBox.ValueChanged += ExchangeRateNumberBox_ValueChanged;
        UpdateFxVisibility();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(SettleAssetAmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(ExchangeRateNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += SettleDebtDialog_PrimaryButtonClick;
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
                SettleAssetAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private void SettleAssetAmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingFx) return;
        _isUpdatingFx = true;
        try
        {
            var amount = AmountNumberBox.Value;
            var settle = SettleAssetAmountNumberBox.Value;
            if (!double.IsNaN(amount) && amount > 0 && !double.IsNaN(settle) && settle > 0)
            {
                ExchangeRateNumberBox.Value = Math.Round(settle / amount, 6);
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
                SettleAssetAmountNumberBox.Value = Math.Round(amount * rate, 2);
            }
        }
        finally
        {
            _isUpdatingFx = false;
        }
    }

    private async void UpdateFxVisibility()
    {
        if (AssetComboBox.SelectedItem is Asset selAsset)
        {
            var debtCurr = !string.IsNullOrWhiteSpace(_debt.Currency) ? _debt.Currency.ToUpperInvariant() : "INR";
            var assetCurr = !string.IsNullOrWhiteSpace(selAsset.Currency) ? selAsset.Currency.ToUpperInvariant() : "INR";
            var assetSymbol = CountryCurrencyData.GetCurrencySymbol(assetCurr);

            var isCrossCurrency = !string.Equals(debtCurr, assetCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency)
            {
                FxPanel.Visibility = Visibility.Visible;
                FxExplanationText.Text = $"Cross-currency settlement ({debtCurr} debt settled via {assetCurr} account). Custom account amount or exchange rate is optional; backend applies live rates if omitted.";
                SettleAssetAmountLabel.Text = _debt.IsLent
                    ? $"Account Amount Credited ({assetSymbol} {assetCurr})"
                    : $"Account Amount Debited ({assetSymbol} {assetCurr})";
                SettleAssetAmountNumberBox.PlaceholderText = $"0.00 ({assetSymbol})";
                ExchangeRateLabel.Text = $"Exchange Rate ({assetCurr} per 1 {debtCurr})";

                try
                {
                    var fxService = App.GetService<IFxRateService>();
                    if (fxService != null)
                    {
                        var liveRate = await fxService.GetExchangeRateAsync(debtCurr, assetCurr);
                        if (liveRate.HasValue && liveRate.Value > 0)
                        {
                            ExchangeRateNumberBox.PlaceholderText = $"Live: 1 {debtCurr} = {liveRate.Value:F4} {assetCurr}";
                            if (double.IsNaN(SettleAssetAmountNumberBox.Value))
                            {
                                var amt = AmountNumberBox.Value;
                                if (!double.IsNaN(amt) && amt > 0)
                                {
                                    SettleAssetAmountNumberBox.PlaceholderText = $"~ {assetSymbol}{amt * liveRate.Value:N2}";
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

    private void SettleDebtDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            ShowError("Please select an Account.");
            AssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            ShowError("Please enter a valid settlement amount greater than 0.");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        if (amount > _debt.RemainingAmount + 0.01)
        {
            args.Cancel = true;
            ShowError($"Settlement amount cannot exceed remaining balance ({_debt.CurrencySymbol} {_debt.RemainingAmount:N2}).");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        double? settleAssetAmount = null;
        double? exchangeRate = null;

        var isCrossCurrency = !string.Equals(_debt.Currency, selectedAsset.Currency, StringComparison.OrdinalIgnoreCase);
        if (isCrossCurrency)
        {
            var sa = SettleAssetAmountNumberBox.Value;
            if (!double.IsNaN(sa))
            {
                if (sa <= 0)
                {
                    args.Cancel = true;
                    ShowError("Converted account amount must be greater than 0.");
                    SettleAssetAmountNumberBox.Focus(FocusState.Programmatic);
                    return;
                }
                settleAssetAmount = sa;
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

        // If paying a borrowed debt, verify sufficient funds
        if (!_debt.IsLent)
        {
            var requiredFunds = settleAssetAmount ?? amount;
            if (selectedAsset.Value < requiredFunds)
            {
                args.Cancel = true;
                ShowError($"Insufficient funds in {selectedAsset.Name}. Available: {selectedAsset.Currency} {selectedAsset.Value:N2}");
                AmountNumberBox.Focus(FocusState.Programmatic);
                return;
            }
        }

        Result = new SettleDebtRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id,
            SettleAssetAmount = settleAssetAmount,
            ExchangeRate = exchangeRate
        };
    }
}
