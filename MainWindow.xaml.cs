using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Services;
using Windows.UI;

namespace TheRichScript_WinUI;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Raised when the hamburger button in the title bar is clicked.
    /// ShellPage subscribes to this to toggle NavigationView.IsPaneOpen.
    /// </summary>
    public event Action? HamburgerToggleRequested;

    public Frame NavigationRootFrame => RootFrame;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        var navService = App.GetService<INavigationService>();
        navService.Initialize(RootFrame, RootFrame);
        navService.CanGoBackChanged += UpdateBackButtonState;
        UpdateBackButtonState(navService.CanGoBack);
    }

    public void UpdateBackButtonState(bool canGoBack)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (AppTitleBar != null)
            {
                AppTitleBar.IsBackButtonEnabled = canGoBack;
            }
        });
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        var navService = App.GetService<INavigationService>();
        if (navService.CanGoBack)
        {
            navService.GoBack();
        }
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        HamburgerToggleRequested?.Invoke();
    }

    private void RootGrid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Left && e.KeyStatus.IsMenuKeyDown)
        {
            var navService = App.GetService<INavigationService>();
            if (navService.CanGoBack)
            {
                navService.GoBack();
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.GoBack)
        {
            var navService = App.GetService<INavigationService>();
            if (navService.CanGoBack)
            {
                navService.GoBack();
                e.Handled = true;
            }
        }
    }

    private void RootGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(RootGrid).Properties;
        if (props.IsXButton1Pressed)
        {
            var navService = App.GetService<INavigationService>();
            if (navService.CanGoBack)
            {
                navService.GoBack();
                e.Handled = true;
            }
        }
    }

    public void UpdateTitleBarTheme(ElementTheme theme)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported()) return;

        var titleBar = AppWindow.TitleBar;
        if (titleBar == null) return;

        bool isDark = theme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => Application.Current.RequestedTheme == ApplicationTheme.Dark
        };

        if (isDark)
        {
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 240, 240, 240);
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 140, 140, 140);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(25, 255, 255, 255);
            titleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 25, 25, 25);
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 130, 130, 130);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 0, 0, 0);
            titleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 0, 0, 0);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(35, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 0, 0, 0);
        }
    }

    /// <summary>
    /// Called by ShellPage to show/hide the hamburger button based on NavigationView display mode.
    /// </summary>
    public void SetHamburgerVisible(bool visible)
    {
        if (AppTitleBar != null)
        {
            AppTitleBar.IsPaneToggleButtonVisible = visible;
        }
    }
}
