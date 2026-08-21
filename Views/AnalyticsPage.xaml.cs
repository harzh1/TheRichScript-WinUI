using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class AnalyticsPage : Page
{
    public AnalyticsViewModel ViewModel { get; }

    public AnalyticsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<AnalyticsViewModel>();
        DataContext = ViewModel;

        Loaded += AnalyticsPage_Loaded;
    }

    private async void AnalyticsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAnalyticsDataAsync();
    }

    private void MonthButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string monthStr)
        {
            ViewModel.SetMonthCommand.Execute(monthStr);
            MonthPickerDropDown.Flyout?.Hide();
        }
    }

    private void CurrentMonthButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetCurrentMonthCommand.Execute(null);
        MonthPickerDropDown.Flyout?.Hide();
    }
}
