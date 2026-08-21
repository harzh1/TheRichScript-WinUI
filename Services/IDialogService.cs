using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TheRichScript_WinUI.Services;

public interface IDialogService
{
    void SetXamlRoot(XamlRoot xamlRoot);
    Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog);
    Task ShowMessageAsync(string title, string message, string closeButtonText = "OK");
    Task<bool> ShowConfirmationAsync(string title, string message, string primaryButtonText = "Confirm", string closeButtonText = "Cancel");
    Task ShowErrorAsync(string message, string title = "Error");
}

public class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;

    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (_xamlRoot != null)
        {
            dialog.XamlRoot = _xamlRoot;
        }

        if (App.MainWindowInstance?.Content is FrameworkElement root)
        {
            dialog.RequestedTheme = root.RequestedTheme;
        }

        dialog.DefaultButton = ContentDialogButton.Primary;
        return await dialog.ShowAsync();
    }

    public async Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = closeButtonText
        };

        await ShowDialogAsync(dialog);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string primaryButtonText = "Confirm", string closeButtonText = "Cancel")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowErrorAsync(string message, string title = "Error")
    {
        await ShowMessageAsync(title, message, "Close");
    }
}
