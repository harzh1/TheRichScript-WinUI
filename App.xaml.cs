using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;
using TheRichScript_WinUI.Views;

namespace TheRichScript_WinUI;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;
    public static Window MainWindowInstance { get; private set; } = null!;

    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is not initialized.");
        }
        return _serviceProvider.GetRequiredService<T>();
    }

    public App()
    {
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        InitializeComponent();
        _serviceProvider = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<IAssetService, AssetService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IPlanService, PlanService>();
        services.AddSingleton<IGoalService, GoalService>();
        services.AddSingleton<IDebtService, DebtService>();
        services.AddSingleton<IIncomeSourceService, IncomeSourceService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IDataCacheService, DataCacheService>();
        services.AddSingleton<IBusyIndicatorService, BusyIndicatorService>();

        // ViewModels
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<AssetsViewModel>();
        services.AddTransient<IncomeSourcesViewModel>();
        services.AddTransient<BudgetsPlansViewModel>();
        services.AddTransient<GoalsViewModel>();
        services.AddTransient<DebtsViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    public static ElementTheme GetElementTheme(string? theme)
    {
        return theme?.ToLowerInvariant() switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public static void SetAppTheme(string? theme)
    {
        var elementTheme = GetElementTheme(theme);
        if (MainWindowInstance?.Content is FrameworkElement root)
        {
            root.RequestedTheme = elementTheme;
        }

        (MainWindowInstance as MainWindow)?.UpdateTitleBarTheme(elementTheme);
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var window = new MainWindow();
            MainWindowInstance = window;

            var settingsService = GetService<ISettingsService>();
            var dialogService = GetService<IDialogService>();

            var initialTheme = GetElementTheme(settingsService.Settings.AppTheme);
            window.UpdateTitleBarTheme(initialTheme);

            if (window.Content is FrameworkElement fe)
            {
                fe.RequestedTheme = initialTheme;
                settingsService.ThemeChanged += (newTheme) =>
                {
                    var updated = GetElementTheme(newTheme);
                    fe.RequestedTheme = updated;
                    window.UpdateTitleBarTheme(updated);
                };

                dialogService.SetXamlRoot(fe.XamlRoot);
                fe.Loaded += (s, e) => dialogService.SetXamlRoot(fe.XamlRoot);
            }

            window.Activate();

            var authService = GetService<IAuthService>();
            var navService = GetService<INavigationService>();
            var dataCacheService = GetService<IDataCacheService>();

            var autoLoggedIn = await authService.AutoLoginAsync();
            if (autoLoggedIn)
            {
                _ = dataCacheService.WarmUpAllDataAsync();
                navService.NavigateRoot(typeof(ShellPage));
            }
            else
            {
                navService.NavigateRoot(typeof(LoginPage));
            }
        }
        catch (Exception ex)
        {
            LogCrash(ex);
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogCrash(ex);
        }
    }

    private static void LogCrash(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var logPath = AppStorageHelper.GetFilePath("crash.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch { }
    }
}
