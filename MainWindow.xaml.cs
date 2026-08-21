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

    private void TitleBarMenuButton_Click(object sender, RoutedEventArgs e)
    {
        HamburgerToggleRequested?.Invoke();
    }

    /// <summary>
    /// Called by ShellPage to show/hide the hamburger button based on NavigationView display mode.
    /// </summary>
    public void SetHamburgerVisible(bool visible)
    {
        TitleBarMenuButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
