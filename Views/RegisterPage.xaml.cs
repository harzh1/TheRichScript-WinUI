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

    private void PasswordBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            RegisterButton_Click(sender, e);
            if (ViewModel.RegisterCommand.CanExecute(null))
            {
                ViewModel.RegisterCommand.Execute(null);
            }
        }
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FirstName = FirstNameBox.Text;
        ViewModel.LastName = LastNameBox.Text;
        ViewModel.Email = EmailBox.Text;
        ViewModel.Password = PasswordBox.Password;
        if (CountryComboBox.SelectedItem is TheRichScript_WinUI.Models.CountryInfo country)
        {
            ViewModel.SelectedCountry = country;
        }
        if (CurrencyComboBox.SelectedItem is TheRichScript_WinUI.Models.CurrencyInfo currency)
        {
            ViewModel.SelectedCurrency = currency;
        }
    }
}
