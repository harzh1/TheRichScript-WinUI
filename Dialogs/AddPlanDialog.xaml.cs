using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddPlanDialog : ContentDialog
{
    private readonly List<string> _availableCategories;

    private const string CreateNewCategoryOption = "+ Create New Category...";

    public CreatePlanRequest? Result { get; private set; }

    public AddPlanDialog(List<Asset> assets, List<string> categories, string defaultType = "RECURRING", PlannedExpense? existingPlan = null)
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
            Title = existingPlan.Type == "MANUAL" ? "Edit Quick Add Template" : "Edit Recurring Plan";
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

            if (existingPlan.Type == "MANUAL")
            {
                ScheduleRadioButtons.SelectedIndex = 1;
            }
            else
            {
                ScheduleRadioButtons.SelectedIndex = 0;
            }

            if (existingPlan.TxType == "INCOME")
            {
                TxTypeRadioButtons.SelectedIndex = 1;
            }
            else
            {
                TxTypeRadioButtons.SelectedIndex = 0;
            }

            if (existingPlan.NextDueDate.HasValue)
            {
                DatePicker.Date = existingPlan.NextDueDate.Value;
            }
        }
        else if (defaultType == "MANUAL")
        {
            Title = "New Quick Add Template";
            ScheduleRadioButtons.SelectedIndex = 1;
        }

        UpdateVisibility();
        PrimaryButtonClick += AddPlanDialog_PrimaryButtonClick;
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

    private void ScheduleRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (FrequencyPanel == null || DueDatePanel == null) return;
        var isRecurring = (ScheduleRadioButtons?.SelectedItem as RadioButton)?.Tag?.ToString() == "RECURRING";
        FrequencyPanel.Visibility = isRecurring ? Visibility.Visible : Visibility.Collapsed;
        DueDatePanel.Visibility = isRecurring ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddPlanDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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

        var isRecurring = (ScheduleRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "RECURRING";
        var isIncome = (TxTypeRadioButtons.SelectedItem as RadioButton)?.Tag?.ToString() == "INCOME";
        var freq = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";

        Result = new CreatePlanRequest
        {
            Name = name,
            Amount = amount,
            AssetId = selectedAsset.Id,
            Category = category,
            Type = isRecurring ? "RECURRING" : "MANUAL",
            TxType = isIncome ? "INCOME" : "EXPENSE",
            Frequency = isRecurring ? freq : null,
            NextDueDate = isRecurring ? (DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(1)) : null
        };
    }
}
