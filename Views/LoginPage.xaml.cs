using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TheRichScript_WinUI.ViewModels;
using Windows.System;

namespace TheRichScript_WinUI.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<LoginViewModel>();
        DataContext = ViewModel;
        Loaded += (s, e) => (App.MainWindowInstance as MainWindow)?.SetHamburgerVisible(false);
    }

    private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBox.Password;
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.Email = EmailBox.Text;
        ViewModel.Password = PasswordBox.Password;

        if (e.Key == VirtualKey.Enter)
        {
            if (ViewModel.LoginCommand.CanExecute(null))
            {
                ViewModel.LoginCommand.Execute(null);
            }
        }
    }

    private void EmailBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            PasswordBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
    }

    private void SignInButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Email = EmailBox.Text;
        ViewModel.Password = PasswordBox.Password;
    }
}
