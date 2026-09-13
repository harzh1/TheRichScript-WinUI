using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;

using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddRecurringPlanDialog : ContentDialog
{
    private readonly List<string> _availableCategories;
    private const string CreateNewCategoryOption = "+ Create New Category...";
    private bool _isInitializing = true;

    public CreatePlanRequest? Result { get; private set; }

    public AddRecurringPlanDialog(List<Asset> assets, List<string> categories, PlannedExpense? existingPlan = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _availableCategories = categories ?? new List<string>();

        FromAssetComboBox.ItemsSource = assets;
        ToAssetComboBox.ItemsSource = assets;

        if (assets.Count > 0)
        {
            FromAssetComboBox.SelectedIndex = 0;
            if (assets.Count > 1)
            {
                ToAssetComboBox.SelectedIndex = 1;
            }
            else
            {
                ToAssetComboBox.SelectedIndex = 0;
            }
        }

        var catList = new List<string>(_availableCategories);
        if (!catList.Contains("INVESTMENT", StringComparer.OrdinalIgnoreCase))
        {
            catList.Insert(0, "INVESTMENT");
        }
        if (!catList.Contains(CreateNewCategoryOption))
        {
            catList.Add(CreateNewCategoryOption);
        }

        CategoryComboBox.ItemsSource = catList;
        if (catList.Count > 0)
        {
            // Default to INVESTMENT if present
            var invCat = catList.FirstOrDefault(c => string.Equals(c, "INVESTMENT", StringComparison.OrdinalIgnoreCase));
            CategoryComboBox.SelectedItem = invCat ?? catList[0];
        }

        // Default new recurring plan to Today so first transfer starts immediately
        DatePicker.Date = DateTimeOffset.Now.Date;

        if (existingPlan != null)
        {
            Title = "Edit Recurring Plan";
            NameBox.Text = existingPlan.Name;
            AmountNumberBox.Value = existingPlan.Amount;

            var matchFrom = assets.FirstOrDefault(a => a.Id == existingPlan.AssetId);
            if (matchFrom != null)
            {
                FromAssetComboBox.SelectedItem = matchFrom;
            }

            var matchTo = assets.FirstOrDefault(a => a.Id == existingPlan.TargetAssetId);
            if (matchTo != null)
            {
                ToAssetComboBox.SelectedItem = matchTo;
            }

            if (_availableCategories.Contains(existingPlan.Category))
            {
                CategoryComboBox.SelectedItem = existingPlan.Category;
            }
            else if (!string.IsNullOrWhiteSpace(existingPlan.Category))
            {
                CategoryComboBox.SelectedItem = CreateNewCategoryOption;
                NewCategoryTextBox.Text = existingPlan.Category;
                NewCategoryTextBox.Visibility = Visibility.Visible;
            }

            var freq = existingPlan.Frequency?.ToUpperInvariant() ?? "MONTHLY";
            foreach (ComboBoxItem item in FrequencyComboBox.Items)
            {
                if (item.Tag?.ToString() == freq)
                {
                    FrequencyComboBox.SelectedItem = item;
                    break;
                }
            }

            if (existingPlan.NextDueDate.HasValue)
            {
                DatePicker.Date = existingPlan.NextDueDate.Value;
            }
        }

        _isInitializing = false;
        UpdateNextCycleButtonLabel();
        UpdateDateSummary();
        UpdateCurrencyDisplay();

        NumericInputHelper.Attach(AmountNumberBox, allowDecimals: true, allowNegative: false);

        PrimaryButtonClick += AddRecurringPlanDialog_PrimaryButtonClick;
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

    private string GetSelectedFrequency()
    {
        return (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";
    }

    private void UpdateNextCycleButtonLabel()
    {
        if (NextCycleButton == null) return;
        var freq = GetSelectedFrequency();
        NextCycleButton.Content = freq switch
        {
            "DAILY" => "Tomorrow",
            "WEEKLY" => "In 1 Week",
            "YEARLY" => "In 1 Year",
            _ => "In 1 Month"
        };
    }

    private void UpdateDateSummary()
    {
        if (ScheduleSummaryText == null) return;
        var freq = GetSelectedFrequency();
        if (!DatePicker.Date.HasValue)
        {
            ScheduleSummaryText.Text = "Please select a transfer date.";
            return;
        }

        var dt = DatePicker.Date.Value.Date;
        var today = DateTime.Today;
        string dateDesc;
        if (dt == today) dateDesc = "Today";
        else if (dt == today.AddDays(1)) dateDesc = "Tomorrow";
        else if (dt < today) dateDesc = $"{dt:dd MMM yyyy} (Overdue - will process immediately)";
        else dateDesc = dt.ToString("dd MMM yyyy");

        var freqDesc = freq switch
        {
            "DAILY" => "every day",
            "WEEKLY" => "every week",
            "YEARLY" => "every year",
            _ => "every month"
        };

        ScheduleSummaryText.Text = $"First transfer: {dateDesc}, then repeats {freqDesc}.";
    }

    private void FrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateNextCycleButtonLabel();
        if (_isInitializing) return;

        var freq = GetSelectedFrequency();
        // If user actively changed frequency, default Daily/Weekly/Monthly to Today so it executes immediately
        if (freq == "DAILY")
        {
            DatePicker.Date = DateTimeOffset.Now.Date;
        }
        else if (!DatePicker.Date.HasValue)
        {
            DatePicker.Date = DateTimeOffset.Now.Date;
        }

        UpdateDateSummary();
    }

    private void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_isInitializing) return;
        UpdateDateSummary();
    }

    private void SetDateToday_Click(object sender, RoutedEventArgs e)
    {
        DatePicker.Date = DateTimeOffset.Now.Date;
    }

    private void SetDateNextCycle_Click(object sender, RoutedEventArgs e)
    {
        var freq = GetSelectedFrequency();
        var baseDate = DateTimeOffset.Now.Date;
        DatePicker.Date = freq switch
        {
            "DAILY" => baseDate.AddDays(1),
            "WEEKLY" => baseDate.AddDays(7),
            "YEARLY" => baseDate.AddYears(1),
            _ => baseDate.AddMonths(1)
        };
    }

    private void AssetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FromAssetComboBox.SelectedItem is Asset fromAsset && ToAssetComboBox.SelectedItem is Asset toAsset)
        {
            AssetValidationWarning.Visibility = (fromAsset.Id == toAsset.Id) ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            AssetValidationWarning.Visibility = Visibility.Collapsed;
        }

        UpdateCurrencyDisplay();
    }

    private void UpdateCurrencyDisplay()
    {
        string? currency = null;
        if (FromAssetComboBox.SelectedItem is Asset fromAsset && !string.IsNullOrWhiteSpace(fromAsset.Currency))
        {
            currency = fromAsset.Currency;
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

        if (AmountLabel != null)
        {
            AmountLabel.Text = $"Transfer Amount ({symbol})";
        }
        if (AmountNumberBox != null)
        {
            AmountNumberBox.PlaceholderText = $"0.00 ({symbol})";
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

    private void AddRecurringPlanDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            ShowError("Plan Name is required.");
            NameBox.Focus(FocusState.Programmatic);
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

        if (FromAssetComboBox.SelectedItem is not Asset fromAsset)
        {
            args.Cancel = true;
            ShowError("Please select a Source Account (Transfer From).");
            FromAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        if (ToAssetComboBox.SelectedItem is not Asset toAsset)
        {
            args.Cancel = true;
            ShowError("Please select a Destination Account (Transfer To).");
            ToAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        if (fromAsset.Id == toAsset.Id)
        {
            AssetValidationWarning.Visibility = Visibility.Visible;
            args.Cancel = true;
            ShowError("Source and Destination accounts cannot be the same.");
            ToAssetComboBox.Focus(FocusState.Programmatic);
            return;
        }

        string category = string.Empty;
        if (CategoryComboBox.SelectedItem is string selectedCat)
        {
            if (selectedCat == CreateNewCategoryOption)
            {
                category = NewCategoryTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(category))
                {
                    args.Cancel = true;
                    ShowError("Please enter a category name.");
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
            category = "INVESTMENT";
        }

        var freq = GetSelectedFrequency();
        var selectedDate = DatePicker.Date?.Date ?? DateTime.Today;

        Result = new CreatePlanRequest
        {
            Name = name,
            Amount = amount,
            AssetId = fromAsset.Id,
            TargetAssetId = toAsset.Id,
            Category = category,
            Type = "RECURRING",
            TxType = "TRANSFER",
            Frequency = freq,
            NextDueDate = selectedDate
        };
    }
}
