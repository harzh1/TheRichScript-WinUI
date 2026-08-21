using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class RegisterPage : Page
{
    public RegisterViewModel ViewModel { get; }

    public RegisterPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<RegisterViewModel>();
        DataContext = ViewModel;
        Loaded += (s, e) => (App.MainWindowInstance as MainWindow)?.SetHamburgerVisible(false);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBox.Password;
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FirstName = FirstNameBox.Text;
        ViewModel.LastName = LastNameBox.Text;
        ViewModel.Email = EmailBox.Text;
        ViewModel.Password = PasswordBox.Password;
        ViewModel.Country = CountryBox.Text;
        ViewModel.Currency = CurrencyBox.Text;
    }
}
