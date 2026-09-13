using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;

using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddCategoryDialog : ContentDialog
{
    public CreateCategoryRequest? Result { get; private set; }

    public AddCategoryDialog(Category? categoryToEdit = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        var symbol = "₹";
        try
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
        catch { }

        MonthlyLimitLabel.Text = $"Monthly Budget Limit ({symbol}) (Optional, 0 for none)";
        YearlyLimitLabel.Text = $"Yearly Budget Limit ({symbol}) (Optional, 0 for none)";
        MonthlyLimitNumberBox.PlaceholderText = $"0.00 ({symbol})";
        YearlyLimitNumberBox.PlaceholderText = $"0.00 ({symbol})";

        NumericInputHelper.Attach(MonthlyLimitNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(YearlyLimitNumberBox, allowDecimals: true, allowNegative: false);

        if (categoryToEdit != null)
        {
            Title = "Edit Category";
            NameBox.Text = categoryToEdit.Name;
            if (categoryToEdit.MonthlyLimit.HasValue) MonthlyLimitNumberBox.Value = categoryToEdit.MonthlyLimit.Value;
            if (categoryToEdit.YearlyLimit.HasValue) YearlyLimitNumberBox.Value = categoryToEdit.YearlyLimit.Value;
        }

        PrimaryButtonClick += AddCategoryDialog_PrimaryButtonClick;
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

    private void AddCategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        var name = NameBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            ShowError("Category Name is required.");
            NameBox.Focus(FocusState.Programmatic);
            return;
        }

        var monthly = MonthlyLimitNumberBox.Value;
        if (!double.IsNaN(monthly) && monthly < 0)
        {
            args.Cancel = true;
            ShowError("Monthly budget limit cannot be negative.");
            MonthlyLimitNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        var yearly = YearlyLimitNumberBox.Value;
        if (!double.IsNaN(yearly) && yearly < 0)
        {
            args.Cancel = true;
            ShowError("Yearly budget limit cannot be negative.");
            YearlyLimitNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        Result = new CreateCategoryRequest
        {
            Name = name,
            MonthlyLimit = (!double.IsNaN(monthly) && monthly > 0) ? monthly : null,
            YearlyLimit = (!double.IsNaN(yearly) && yearly > 0) ? yearly : null
        };
    }
}
