using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class FundGoalDialog : ContentDialog
{
    private readonly Goal _goal;
    private bool _isUpdatingFx;
    public AddGoalFundsRequest? Result { get; private set; }

    public FundGoalDialog(Goal goal, List<Asset> assets)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _goal = goal;

        var symbol = goal.CurrencySymbol;
        GoalInfoText.Text = $"Goal: {goal.Name}\nRemaining Target: {symbol} {goal.RemainingAmount:N2}";

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        var targetAsset = assets.Find(a => a.Id == goal.TargetAssetId);
        if (targetAsset != null)
        {
            DestinationAccountText.Text = $"Transfers to: {targetAsset.DisplayNameWithBalance}";
        }
        else
        {
            DestinationAccountText.Text = "No deposit account linked. (Edit this goal to select a Deposit Account).";
        }

        AssetComboBox.SelectionChanged += (_, _) => UpdateFxVisibility();
        AmountNumberBox.ValueChanged += AmountNumberBox_ValueChanged;
        DestAmountNumberBox.ValueChanged += DestAmountNumberBox_ValueChanged;
        ExchangeRateNumberBox.ValueChanged += ExchangeRateNumberBox_ValueChanged;
        UpdateFxVisibility();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(DestAmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(ExchangeRateNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += FundGoalDialog_PrimaryButtonClick;
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
                var computedDest = Math.Min(_goal.RemainingAmount, Math.Round(amount * rate, 2));
                DestAmountNumberBox.Value = computedDest;
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
            var dest = DestAmountNumberBox.Value;
            var rate = ExchangeRateNumberBox.Value;
            var amount = AmountNumberBox.Value;

            if (!double.IsNaN(dest) && dest > 0)
            {
                if (!double.IsNaN(rate) && rate > 0)
                {
                    AmountNumberBox.Value = Math.Round(dest / rate, 2);
                }
                else if (!double.IsNaN(amount) && amount > 0)
                {
                    ExchangeRateNumberBox.Value = Math.Round(dest / amount, 6);
                }
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
            var rate = ExchangeRateNumberBox.Value;
            var dest = DestAmountNumberBox.Value;
            var amount = AmountNumberBox.Value;

            if (!double.IsNaN(rate) && rate > 0)
            {
                if (!double.IsNaN(dest) && dest > 0)
                {
                    AmountNumberBox.Value = Math.Round(dest / rate, 2);
                }
                else if (!double.IsNaN(amount) && amount > 0)
                {
                    DestAmountNumberBox.Value = Math.Min(_goal.RemainingAmount, Math.Round(amount * rate, 2));
                }
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
            var goalCurr = !string.IsNullOrWhiteSpace(_goal.Currency) ? _goal.Currency.ToUpperInvariant() : "INR";
            var assetCurr = !string.IsNullOrWhiteSpace(selAsset.Currency) ? selAsset.Currency.ToUpperInvariant() : "INR";
            var assetSymbol = CountryCurrencyData.GetCurrencySymbol(assetCurr);
            var goalSymbol = CountryCurrencyData.GetCurrencySymbol(goalCurr);

            AmountLabel.Text = $"Deposit Amount ({assetSymbol} {assetCurr})";
            AmountNumberBox.PlaceholderText = $"0.00 ({assetSymbol})";

            var isCrossCurrency = !string.Equals(goalCurr, assetCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency)
            {
                FxPanel.Visibility = Visibility.Visible;
                FxExplanationText.Text = $"Cross-currency deposit ({assetCurr} account funding {goalCurr} goal). Custom credited amount or exchange rate is optional; backend applies live rates if omitted.";
                DestAmountLabel.Text = $"Goal Amount Credited ({goalSymbol} {goalCurr})";
                DestAmountNumberBox.PlaceholderText = $"0.00 ({goalSymbol})";
                DestAmountNumberBox.Maximum = _goal.RemainingAmount;
                if (double.IsNaN(DestAmountNumberBox.Value) || DestAmountNumberBox.Value <= 0)
                {
                    DestAmountNumberBox.Value = _goal.RemainingAmount;
                }

                AmountNumberBox.Maximum = selAsset.Value;

                ExchangeRateLabel.Text = $"Exchange Rate ({goalCurr} per 1 {assetCurr})";

                try
                {
                    var fxService = App.GetService<IFxRateService>();
                    if (fxService != null)
                    {
                        var liveRate = await fxService.GetExchangeRateAsync(assetCurr, goalCurr);
                        if (liveRate.HasValue && liveRate.Value > 0)
                        {
                            ExchangeRateNumberBox.PlaceholderText = $"Live: 1 {assetCurr} = {liveRate.Value:F4} {goalCurr}";
                            if (double.IsNaN(ExchangeRateNumberBox.Value))
                            {
                                // Suggest the source deposit needed to cover the goal remaining amount
                                var neededSource = Math.Round(_goal.RemainingAmount / liveRate.Value, 2);
                                AmountNumberBox.PlaceholderText = $"~ {assetSymbol}{neededSource:N2}";
                                if (double.IsNaN(AmountNumberBox.Value) || AmountNumberBox.Value <= 0)
                                {
                                    AmountNumberBox.Value = Math.Min(neededSource, selAsset.Value);
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
                AmountNumberBox.Maximum = Math.Min(_goal.RemainingAmount, selAsset.Value);
                if (double.IsNaN(AmountNumberBox.Value) || AmountNumberBox.Value <= 0)
                {
                    AmountNumberBox.Value = Math.Min(_goal.RemainingAmount, selAsset.Value);
                }
            }
        }
    }

    private void FundGoalDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        if (AssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            ShowError("Please select a Source Account.");
            AssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var amount = AmountNumberBox.Value;
        if (double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            ShowError("Please enter a valid deposit amount greater than 0.");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        if (selectedAsset.Value < amount)
        {
            args.Cancel = true;
            ShowError($"Insufficient funds in {selectedAsset.Name}. Available: {selectedAsset.Currency} {selectedAsset.Value:N2}");
            AmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        double? destAmount = null;
        double? exchangeRate = null;

        var isCrossCurrency = !string.Equals(_goal.Currency, selectedAsset.Currency, StringComparison.OrdinalIgnoreCase);
        if (isCrossCurrency)
        {
            var da = DestAmountNumberBox.Value;
            if (!double.IsNaN(da))
            {
                if (da <= 0)
                {
                    args.Cancel = true;
                    ShowError("Goal credited amount must be greater than 0.");
                    DestAmountNumberBox.Focus(FocusState.Programmatic);
                    return;
                }
                if (da > _goal.RemainingAmount + 0.01)
                {
                    args.Cancel = true;
                    ShowError($"Goal credited amount cannot exceed remaining target ({_goal.Currency} {_goal.RemainingAmount:N2}).");
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

        Result = new AddGoalFundsRequest
        {
            Amount = amount,
            AssetId = selectedAsset.Id,
            DestinationAmount = destAmount,
            ExchangeRate = exchangeRate
        };
    }
}
