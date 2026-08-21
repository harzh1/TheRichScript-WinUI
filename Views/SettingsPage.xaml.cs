using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;

        Loaded += (s, e) => ViewModel.LoadCurrentSettings();
    }

    private async void Avatar_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PreviewAvatarAsync(this.XamlRoot);
    }

    private async void UploadAvatar_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.UploadAvatarAsync(this.XamlRoot);
    }

    private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.CurrentPassword = CurrentPasswordBox.Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.NewPassword = NewPasswordBox.Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is string theme && !string.IsNullOrEmpty(theme))
        {
            ViewModel.SelectedTheme = theme;
            ViewModel.SaveThemePreference();
        }
    }
}
