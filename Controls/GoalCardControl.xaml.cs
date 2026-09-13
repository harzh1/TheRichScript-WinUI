using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.Controls;

public sealed partial class GoalCardControl : UserControl
{
    private static readonly Dictionary<string, BitmapImage> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SolidColorBrush DotActiveBrush = new(Microsoft.UI.Colors.White);
    private static readonly SolidColorBrush DotInactiveBrush = new(Microsoft.UI.ColorHelper.FromArgb(128, 255, 255, 255));

    public static BitmapImage? GetOrCreateBitmap(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (_imageCache.TryGetValue(url, out var cached))
        {
            return cached;
        }

        try
        {
            var bitmap = new BitmapImage
            {
                UriSource = new Uri(url),
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = 530 // 2x retina display scaling for ~265px card width, saves memory & decodes instantly
            };
            _imageCache[url] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private DispatcherTimer? _slideshowTimer;
    private Storyboard? _slideStoryboard;
    private bool _isSliding;
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
            if (e.OldValue is System.ComponentModel.INotifyPropertyChanged oldObj)
            {
                oldObj.PropertyChanged -= control.OnGoalModelPropertyChanged;
            }
            if (e.NewValue is System.ComponentModel.INotifyPropertyChanged newObj)
            {
                newObj.PropertyChanged += control.OnGoalModelPropertyChanged;
            }
            control.UpdateUI();
        }
    }

    private void OnGoalModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue?.TryEnqueue(UpdateUI);
    }

    private void GoalCardControl_Loaded(object sender, RoutedEventArgs e)
    {
        StartSlideshow();
    }

    private void GoalCardControl_Unloaded(object sender, RoutedEventArgs e)
    {
        StopSlideshow();
        if (Goal is System.ComponentModel.INotifyPropertyChanged notify)
        {
            notify.PropertyChanged -= OnGoalModelPropertyChanged;
        }
    }

    private string GetEffectiveCurrencySymbol()
    {
        if (!string.IsNullOrWhiteSpace(Goal?.Currency))
        {
            return CountryCurrencyData.GetCurrencySymbol(Goal.Currency);
        }

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
        GoalNameText.Text = Goal.Name;
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
            if (Goal.ShowBaseValue && Goal.BaseTargetAmount > 0)
            {
                var baseSymbol = CountryCurrencyData.GetCurrencySymbol(Goal.BaseCurrency);
                var baseRemaining = Math.Max(0, Goal.BaseTargetAmount - Goal.BaseCurrentAmount);
                RemainingAmountText.Text = $"{symbol}{Goal.RemainingAmount:N2} remaining (≈ {baseSymbol}{baseRemaining:N0})";
            }
            else
            {
                RemainingAmountText.Text = $"{symbol}{Goal.RemainingAmount:N2} remaining";
            }

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
        PinMenuIcon.Glyph = pinGlyph;
        PinnedBadge.Visibility = Goal.IsPinned ? Visibility.Visible : Visibility.Collapsed;
        PinMenuItem.Text = Goal.IsPinned ? "Unpin Goal" : "Pin Goal";

        // Completed states
        CompletedBadge.Visibility = Goal.IsCompleted ? Visibility.Visible : Visibility.Collapsed;

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
            // Eagerly preload and cache all images into memory upfront
            for (int i = 0; i < _images.Count; i++)
            {
                GetOrCreateBitmap(_images[i]);
            }

            FallbackBackgroundGrid.Visibility = Visibility.Collapsed;
            SlideshowViewport.Visibility = Visibility.Visible;
            ImageCurrent.Visibility = Visibility.Visible;
            _currentImageIndex = 0;

            ImageCurrent.Source = GetOrCreateBitmap(_images[0]);
            TransformCurrent.X = 0;

            // Pre-arm ImageNext with the upcoming image (index 1) so it is already decoded and warm in GPU memory!
            if (_images.Count > 1)
            {
                ImageNext.Source = GetOrCreateBitmap(_images[1]);
                TransformNext.X = 265.0;
                ImageNext.Opacity = 0;
                ImageNext.Visibility = Visibility.Visible;
            }
            else
            {
                ImageNext.Source = null;
                ImageNext.Opacity = 0;
                ImageNext.Visibility = Visibility.Collapsed;
            }

            BuildDotIndicators();
            StartSlideshow();
        }
        else
        {
            FallbackBackgroundGrid.Visibility = Visibility.Visible;
            SlideshowViewport.Visibility = Visibility.Collapsed;
            ImageCurrent.Source = null;
            ImageNext.Source = null;
            SlideshowDotsBadge.Visibility = Visibility.Collapsed;
            DotsIndicatorPanel.Children.Clear();
            StopSlideshow();
        }
    }

    private void StartSlideshow()
    {
        StopSlideshow();
        if (_images.Count > 1)
        {
            // Calm 3.5s cycle: 1.2s smooth slow sliding + 2.3s display
            _slideshowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _slideshowTimer.Tick += (s, e) =>
            {
                SlideToNextImage();
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
        if (_slideStoryboard != null)
        {
            _slideStoryboard.Stop();
            _slideStoryboard = null;
        }
        _isSliding = false;
        if (TransformCurrent != null) TransformCurrent.X = 0;
        if (TransformNext != null) TransformNext.X = 0;
        if (ImageNext != null)
        {
            ImageNext.Opacity = 0;
            ImageNext.Visibility = Visibility.Collapsed;
        }
    }

    private void SlideToNextImage()
    {
        if (_images.Count <= 1 || _isSliding) return;

        double width = ImageContainer.ActualWidth > 0 ? ImageContainer.ActualWidth : 265.0;
        int nextIndex = (_currentImageIndex + 1) % _images.Count;

        try
        {
            // ImageNext was ALREADY preloaded and decoded into memory 3.5s ago!
            var nextBitmap = GetOrCreateBitmap(_images[nextIndex]);
            if (ImageNext.Source != nextBitmap)
            {
                ImageNext.Source = nextBitmap;
            }

            // Position ImageNext right at the right edge, make it fully opaque
            TransformNext.X = width;
            ImageNext.Opacity = 1;
            ImageNext.Visibility = Visibility.Visible;
            TransformCurrent.X = 0;

            _slideStoryboard = new Storyboard();

            // Smooth slow slide: current image glides left out of frame
            var animCurrent = new DoubleAnimation
            {
                From = 0,
                To = -width,
                Duration = TimeSpan.FromMilliseconds(1200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(animCurrent, TransformCurrent);
            Storyboard.SetTargetProperty(animCurrent, "X");
            _slideStoryboard.Children.Add(animCurrent);

            // Incoming image glides from right into center frame
            var animNext = new DoubleAnimation
            {
                From = width,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(animNext, TransformNext);
            Storyboard.SetTargetProperty(animNext, "X");
            _slideStoryboard.Children.Add(animNext);

            // Update dot indicator to the incoming image immediately
            UpdateDotIndicators(nextIndex);

            _isSliding = true;
            _slideStoryboard.Completed += (s, e) =>
            {
                _currentImageIndex = nextIndex;
                ImageCurrent.Source = ImageNext.Source;
                TransformCurrent.X = 0;

                // Park ImageNext off-screen and hide opacity
                TransformNext.X = width;
                ImageNext.Opacity = 0;
                _isSliding = false;

                // IMMEDIATELY pre-load the upcoming image into ImageNext
                // so during the 2.3s pause, it downloads and decodes into GPU VRAM!
                int upcomingIndex = (_currentImageIndex + 1) % _images.Count;
                ImageNext.Source = GetOrCreateBitmap(_images[upcomingIndex]);
            };

            _slideStoryboard.Begin();
        }
        catch
        {
            _isSliding = false;
        }
    }

    private void BuildDotIndicators()
    {
        DotsIndicatorPanel.Children.Clear();
        if (_images.Count <= 1)
        {
            SlideshowDotsBadge.Visibility = Visibility.Collapsed;
            return;
        }

        SlideshowDotsBadge.Visibility = Visibility.Visible;
        for (int i = 0; i < _images.Count; i++)
        {
            bool isActive = (i == _currentImageIndex);
            var dot = new Border
            {
                Width = isActive ? 14 : 5,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = isActive ? DotActiveBrush : DotInactiveBrush
            };
            DotsIndicatorPanel.Children.Add(dot);
        }
    }

    private void UpdateDotIndicators(int? activeIndex = null)
    {
        var targetIndex = activeIndex ?? _currentImageIndex;
        for (int i = 0; i < DotsIndicatorPanel.Children.Count; i++)
        {
            if (DotsIndicatorPanel.Children[i] is Border dot)
            {
                bool isActive = (i == targetIndex);
                dot.Width = isActive ? 14 : 5;
                dot.Background = isActive ? DotActiveBrush : DotInactiveBrush;
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

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        double width = availableSize.Width;
        if (double.IsInfinity(width) || width <= 0)
        {
            width = 270.0;
        }

        // Top image section strictly enforces 16:9 aspect ratio (Height = Width * 9 / 16)
        double image16x9Height = Math.Round(width * 9.0 / 16.0);
        ImageContainer.Height = image16x9Height;

        // Clip viewport to prevent horizontal slide-in overflow outside card bounds
        SlideshowViewport.Clip = new RectangleGeometry
        {
            Rect = new Windows.Foundation.Rect(0, 0, width, image16x9Height)
        };

        return base.MeasureOverride(availableSize);
    }
}
