using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddCategoryDialog : ContentDialog
{
    public CreateCategoryRequest? Result { get; private set; }

    public AddCategoryDialog(Category? categoryToEdit = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        if (categoryToEdit != null)
        {
            Title = "Edit Category";
            NameBox.Text = categoryToEdit.Name;
            if (categoryToEdit.MonthlyLimit.HasValue) MonthlyLimitNumberBox.Value = categoryToEdit.MonthlyLimit.Value;
            if (categoryToEdit.YearlyLimit.HasValue) YearlyLimitNumberBox.Value = categoryToEdit.YearlyLimit.Value;
        }

        PrimaryButtonClick += AddCategoryDialog_PrimaryButtonClick;
    }

    private void AddCategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            return;
        }

        var monthly = MonthlyLimitNumberBox.Value;
        var yearly = YearlyLimitNumberBox.Value;

        Result = new CreateCategoryRequest
        {
            Name = name,
            MonthlyLimit = (!double.IsNaN(monthly) && monthly > 0) ? monthly : null,
            YearlyLimit = (!double.IsNaN(yearly) && yearly > 0) ? yearly : null
        };
    }
}
