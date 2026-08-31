using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class PasswordConfirmationDialog : ContentDialog
{
    public string ConfirmedPassword { get; private set; } = string.Empty;

    public PasswordConfirmationDialog(
        string title,
        string message,
        string primaryButtonText = "Confirm",
        string warningTitle = "Permanent Action Warning",
        string warningMessage = "This action is permanent and cannot be undone.")
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        Title = title;
        MessageTextBlock.Text = message;
        PrimaryButtonText = primaryButtonText;
        WarningInfoBar.Title = warningTitle;
        WarningInfoBar.Message = warningMessage;

        PrimaryButtonClick += PasswordConfirmationDialog_PrimaryButtonClick;
    }

    private void PasswordConfirmationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var password = PasswordInputBox.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            ValidationErrorText.Text = "Please enter your password to confirm.";
            ValidationErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ValidationErrorText.Visibility = Visibility.Collapsed;
        ConfirmedPassword = password;
    }
}
