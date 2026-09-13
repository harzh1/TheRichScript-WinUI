using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddSubscriptionDialog : ContentDialog
{
    private readonly List<string> _availableCategories;
    private const string CreateNewCategoryOption = "+ Create New Category...";

    public CreatePlanRequest? Result { get; private set; }

    public AddSubscriptionDialog(List<Asset> assets, List<string> categories, PlannedExpense? existingPlan = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _availableCategories = categories ?? new List<string>();

        SourceAssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            SourceAssetComboBox.SelectedIndex = 0;
        }

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

        DatePicker.Date = DateTimeOffset.Now.AddMonths(1);

        if (existingPlan != null)
        {
            Title = "Edit Subscription";
            NameBox.Text = existingPlan.Name;
            AmountNumberBox.Value = existingPlan.Amount;

            if (_availableCategories.Contains(existingPlan.Category))
            {
                CategoryComboBox.SelectedItem = existingPlan.Category;
            }
            else
            {
                CategoryComboBox.SelectedItem = CreateNewCategoryOption;
                NewCategoryTextBox.Text = existingPlan.Category;
                NewCategoryTextBox.Visibility = Visibility.Visible;
            }

            var matchAsset = assets.FirstOrDefault(a => a.Id == existingPlan.AssetId);
            if (matchAsset != null)
            {
                SourceAssetComboBox.SelectedItem = matchAsset;
            }

            // Frequency
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

        PrimaryButtonClick += AddSubscriptionDialog_PrimaryButtonClick;
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

    private void AddSubscriptionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameBox.Text.Trim();
        var amount = AmountNumberBox.Value;

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

        if (string.IsNullOrWhiteSpace(name) || double.IsNaN(amount) || amount <= 0 || string.IsNullOrWhiteSpace(category))
        {
            args.Cancel = true;
            return;
        }

        if (SourceAssetComboBox.SelectedItem is not Asset selectedAsset)
        {
            args.Cancel = true;
            return;
        }

        var freq = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";

        Result = new CreatePlanRequest
        {
            Name = name,
            Amount = amount,
            AssetId = selectedAsset.Id,
            TargetAssetId = null,
            Category = category,
            Type = "RECURRING",
            TxType = "EXPENSE",
            Frequency = freq,
            NextDueDate = DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(1)
        };
    }
}
