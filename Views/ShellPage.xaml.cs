using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    public ShellPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<ShellViewModel>();
        DataContext = ViewModel;

        var navService = App.GetService<INavigationService>();
        var rootFrame = (App.MainWindowInstance as MainWindow)?.NavigationRootFrame;
        navService.Initialize(ContentFrame, rootFrame ?? ContentFrame);
        navService.Navigated += NavService_Navigated;

        // Wire the title bar hamburger button to toggle this NavigationView's pane
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.HamburgerToggleRequested += () =>
            {
                NavView.IsPaneOpen = !NavView.IsPaneOpen;
            };
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Sync hamburger visibility on initial load
        UpdateHamburgerVisibility(NavView.DisplayMode);

        // Navigate to dashboard by default
        if (NavView.MenuItems.Count > 0 && NavView.SelectedItem == null)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        UpdateHamburgerVisibility(args.DisplayMode);
    }

    private void UpdateHamburgerVisibility(NavigationViewDisplayMode displayMode)
    {
        // Show the title bar hamburger button only when NOT in Expanded mode
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.SetHamburgerVisible(displayMode != NavigationViewDisplayMode.Expanded);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var navService = App.GetService<INavigationService>();

        if (args.SelectedItemContainer is NavigationViewItem selectedItem && selectedItem.Tag is string tag)
        {
            switch (tag)
            {
                case "Dashboard":
                    navService.NavigateTo(typeof(DashboardPage));
                    break;
                case "Transactions":
                    navService.NavigateTo(typeof(TransactionsPage));
                    break;
                case "Assets":
                    navService.NavigateTo(typeof(AssetsPage));
                    break;
                case "IncomeSources":
                    navService.NavigateTo(typeof(IncomeSourcesPage));
                    break;
                case "BudgetsPlans":
                    navService.NavigateTo(typeof(BudgetsPlansPage));
                    break;
                case "Goals":
                    navService.NavigateTo(typeof(GoalsPage));
                    break;
                case "Debts":
                    navService.NavigateTo(typeof(DebtsPage));
                    break;
                case "Analytics":
                    navService.NavigateTo(typeof(AnalyticsPage));
                    break;
                case "Categories":
                    navService.NavigateTo(typeof(CategoriesPage));
                    break;
                case "Settings":
                    navService.NavigateTo(typeof(SettingsPage));
                    break;
            }
        }
    }

    private void NavService_Navigated(Type? pageType)
    {
        string? matchTag = pageType switch
        {
            _ when pageType == typeof(DashboardPage) => "Dashboard",
            _ when pageType == typeof(TransactionsPage) => "Transactions",
            _ when pageType == typeof(AssetsPage) => "Assets",
            _ when pageType == typeof(IncomeSourcesPage) => "IncomeSources",
            _ when pageType == typeof(BudgetsPlansPage) => "BudgetsPlans",
            _ when pageType == typeof(GoalsPage) => "Goals",
            _ when pageType == typeof(DebtsPage) => "Debts",
            _ when pageType == typeof(AnalyticsPage) => "Analytics",
            _ when pageType == typeof(CategoriesPage) => "Categories",
            _ when pageType == typeof(SettingsPage) => "Settings",
            _ => null
        };

        if (matchTag != null)
        {
            var item = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == matchTag)
                       ?? NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == matchTag);
            if (item != null)
            {
                NavView.SelectedItem = item;
            }
        }
    }
}
