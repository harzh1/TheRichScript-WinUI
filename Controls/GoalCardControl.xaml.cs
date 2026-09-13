using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Controls;

public sealed partial class GoalCardControl : UserControl
{
    private DispatcherTimer? _slideshowTimer;
    private int _currentImageIndex;
    private List<string> _images = new();

    public static readonly DependencyProperty GoalProperty =
        DependencyProperty.Register(
            nameof(Goal),
            typeof(Goal),
            typeof(GoalCardControl),
            new PropertyMetadata(null, OnGoalChanged));

    public Goal? Goal
    {
        get => (Goal?)GetValue(GoalProperty);
        set => SetValue(GoalProperty, value);
    }

    public static readonly DependencyProperty CurrencySymbolProperty =
        DependencyProperty.Register(
            nameof(CurrencySymbol),
            typeof(string),
            typeof(GoalCardControl),
            new PropertyMetadata("₹", OnGoalChanged));

    public string CurrencySymbol
    {
        get => (string)GetValue(CurrencySymbolProperty);
        set => SetValue(CurrencySymbolProperty, value);
    }

    public event Action<Goal>? DepositFundsClicked;
    public event Action<Goal>? TogglePinClicked;
    public event Action<Goal>? EditGoalClicked;
    public event Action<Goal>? DeleteGoalClicked;

    public GoalCardControl()
    {
        InitializeComponent();
        Loaded += GoalCardControl_Loaded;
        Unloaded += GoalCardControl_Unloaded;
    }

    private static void OnGoalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoalCardControl control)
        {
            control.UpdateUI();
        }
    }

    private void GoalCardControl_Loaded(object sender, RoutedEventArgs e)
    {
        StartSlideshow();
    }

    private void GoalCardControl_Unloaded(object sender, RoutedEventArgs e)
    {
        StopSlideshow();
    }

    private string GetEffectiveCurrencySymbol()
    {
        if (!string.IsNullOrEmpty(CurrencySymbol) && CurrencySymbol != "₹")
        {
            return CurrencySymbol;
        }

        try
        {
            var settings = App.GetService<TheRichScript_WinUI.Services.ISettingsService>();
            return settings?.Settings?.CurrencySymbol ?? CurrencySymbol ?? "₹";
        }
        catch
        {
            return CurrencySymbol ?? "₹";
        }
    }

    public void UpdateUI()
    {
        if (Goal == null) return;

        var symbol = GetEffectiveCurrencySymbol();
        GoalNameTextImage.Text = Goal.Name;
        GoalNameTextNoImg.Text = Goal.Name;
        CurrentAmountText.Text = $"{symbol}{Goal.CurrentAmount:N2}";
        TargetAmountText.Text = $"{symbol}{Goal.TargetAmount:N2}";
        GoalProgressBar.Value = Goal.ProgressPercentage;
        ProgressPercentageText.Text = $"{Goal.ProgressPercentage:F0}% reached";
        ExpectedDateText.Text = $"{Goal.ExpectedDate:dd MMM yyyy}";

        if (Goal.IsCompleted)
        {
            RemainingAmountText.Text = "Target reached ✓";
            if (Application.Current.Resources.TryGetValue("IncomeGreenBrush", out var greenBrush) && greenBrush is Brush gBrush)
            {
                RemainingAmountText.Foreground = gBrush;
                GoalProgressBar.Foreground = gBrush;
            }
            CompletedBanner.Visibility = Visibility.Visible;
            DepositFundsButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            RemainingAmountText.Text = $"{symbol}{Goal.RemainingAmount:N2} remaining";
            if (Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var secBrush) && secBrush is Brush sBrush)
            {
                RemainingAmountText.Foreground = sBrush;
            }
            if (Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var accBrush) && accBrush is Brush aBrush)
            {
                GoalProgressBar.Foreground = aBrush;
            }
            CompletedBanner.Visibility = Visibility.Collapsed;
            DepositFundsButton.Visibility = Visibility.Visible;
        }

        // Pin Icons and Badges
        var pinGlyph = Goal.IsPinned ? "\uE840" : "\uE718";
        PinIconImage.Glyph = pinGlyph;
        PinIconNoImg.Glyph = pinGlyph;

        var pinnedVis = Goal.IsPinned ? Visibility.Visible : Visibility.Collapsed;
        PinnedBadgeImage.Visibility = pinnedVis;
        PinnedBadgeNoImg.Visibility = pinnedVis;

        // Completed states
        var completedVis = Goal.IsCompleted ? Visibility.Visible : Visibility.Collapsed;
        CompletedBadgeImage.Visibility = completedVis;
        CompletedBadgeNoImg.Visibility = completedVis;

        // Auto-funding
        if (Goal.RecurringAmount.HasValue && Goal.RecurringAmount.Value > 0)
        {
            AutoFundingBorder.Visibility = Visibility.Visible;
            AutoFundingText.Text = $"Auto: {symbol}{Goal.RecurringAmount.Value:N2} / {Goal.RecurringFrequency?.ToLowerInvariant() ?? "month"}";
        }
        else
        {
            AutoFundingBorder.Visibility = Visibility.Collapsed;
        }

        // Images / Slideshow setup
        _images = Goal.ImageUrls ?? new List<string>();
        if (_images.Count > 0)
        {
            ImageBannerGrid.Visibility = Visibility.Visible;
            NoImageHeaderGrid.Visibility = Visibility.Collapsed;
            _currentImageIndex = 0;
            DisplayCurrentImage();
            BuildDotIndicators();
            StartSlideshow();
        }
        else
        {
            ImageBannerGrid.Visibility = Visibility.Collapsed;
            NoImageHeaderGrid.Visibility = Visibility.Visible;
            DotsIndicatorPanel.Children.Clear();
            StopSlideshow();
        }
    }

    private void StartSlideshow()
    {
        StopSlideshow();
        if (_images.Count > 1)
        {
            _slideshowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _slideshowTimer.Tick += (s, e) =>
            {
                if (_images.Count == 0) return;
                _currentImageIndex = (_currentImageIndex + 1) % _images.Count;
                DisplayCurrentImage();
                UpdateDotIndicators();
            };
            _slideshowTimer.Start();
        }
    }

    private void StopSlideshow()
    {
        if (_slideshowTimer != null)
        {
            _slideshowTimer.Stop();
            _slideshowTimer = null;
        }
    }

    private void DisplayCurrentImage()
    {
        if (_images.Count == 0 || _currentImageIndex >= _images.Count) return;

        try
        {
            var uri = _images[_currentImageIndex];
            SlideshowImage.Source = new BitmapImage(new Uri(uri));
        }
        catch { }
    }

    private void BuildDotIndicators()
    {
        DotsIndicatorPanel.Children.Clear();
        if (_images.Count <= 1) return;

        for (int i = 0; i < _images.Count; i++)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = i == _currentImageIndex
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(100, 255, 255, 255)),
                Margin = new Thickness(1, 0, 1, 0)
            };
            DotsIndicatorPanel.Children.Add(dot);
        }
    }

    private void UpdateDotIndicators()
    {
        for (int i = 0; i < DotsIndicatorPanel.Children.Count; i++)
        {
            if (DotsIndicatorPanel.Children[i] is Ellipse dot)
            {
                dot.Fill = i == _currentImageIndex
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(100, 255, 255, 255));
            }
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (Goal != null)
        {
            TogglePinClicked?.Invoke(Goal);
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (Goal != null)
        {
            EditGoalClicked?.Invoke(Goal);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (Goal != null)
        {
            DeleteGoalClicked?.Invoke(Goal);
        }
    }

    private void DepositFundsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Goal != null)
        {
            DepositFundsClicked?.Invoke(Goal);
        }
    }
}
