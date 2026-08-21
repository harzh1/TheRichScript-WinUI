using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AvatarPreviewDialog : ContentDialog
{
    public bool ChangePhotoRequested { get; private set; }

    public AvatarPreviewDialog(User user)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        UserNameTextBlock.Text = user.FullName;
        UserEmailTextBlock.Text = user.DisplayEmail;
        AvatarPersonPicture.DisplayName = user.FullName;
        AvatarPersonPicture.Initials = user.Initials;

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl) && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out var uri))
        {
            AvatarPersonPicture.ProfilePicture = new BitmapImage(uri);
        }
        else
        {
            FallbackInitials.Text = user.Initials;
            FallbackInitials.Visibility = Visibility.Visible;
        }

        PrimaryButtonClick += (s, e) => ChangePhotoRequested = true;
    }
}
