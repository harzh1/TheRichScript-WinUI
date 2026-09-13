using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddRecurringPlanDialog : ContentDialog
{
    private readonly List<string> _availableCategories;
    private const string CreateNewCategoryOption = "+ Create New Category...";

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

        DatePicker.Date = DateTimeOffset.Now.AddMonths(1);

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

        PrimaryButtonClick += AddRecurringPlanDialog_PrimaryButtonClick;
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
        var name = NameBox.Text.Trim();
        var amount = AmountNumberBox.Value;

        if (FromAssetComboBox.SelectedItem is not Asset fromAsset ||
            ToAssetComboBox.SelectedItem is not Asset toAsset)
        {
            args.Cancel = true;
            return;
        }

        if (fromAsset.Id == toAsset.Id)
        {
            AssetValidationWarning.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        string category = string.Empty;
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
            category = "INVESTMENT";
        }

        if (string.IsNullOrWhiteSpace(name) || double.IsNaN(amount) || amount <= 0)
        {
            args.Cancel = true;
            return;
        }

        var freq = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";

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
            NextDueDate = DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(1)
        };
    }
}
