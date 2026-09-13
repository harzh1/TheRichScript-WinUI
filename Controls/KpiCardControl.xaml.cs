using System.Windows.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace TheRichScript_WinUI.Controls;

public sealed partial class KpiCardControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(KpiCardControl), new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(KpiCardControl), new PropertyMetadata(string.Empty, OnValueChanged));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(KpiCardControl), new PropertyMetadata(string.Empty, OnSubtitleChanged));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(KpiCardControl), new PropertyMetadata("\uE8EC", OnGlyphChanged));

    public static readonly DependencyProperty IconBrushProperty =
        DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(KpiCardControl), new PropertyMetadata(null, OnIconBrushChanged));

    public static readonly DependencyProperty GradientStartColorProperty =
        DependencyProperty.Register(nameof(GradientStartColor), typeof(Color?), typeof(KpiCardControl), new PropertyMetadata(null, OnColorsChanged));

    public static readonly DependencyProperty GradientEndColorProperty =
        DependencyProperty.Register(nameof(GradientEndColor), typeof(Color?), typeof(KpiCardControl), new PropertyMetadata(null, OnColorsChanged));

    public static readonly DependencyProperty ValueColorProperty =
        DependencyProperty.Register(nameof(ValueColor), typeof(Color?), typeof(KpiCardControl), new PropertyMetadata(null, OnValueColorChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(KpiCardControl), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(KpiCardControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowActionIconProperty =
        DependencyProperty.Register(nameof(ShowActionIcon), typeof(bool), typeof(KpiCardControl), new PropertyMetadata(true, OnShowActionIconChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Brush? IconBrush
    {
        get => (Brush?)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public Color? GradientStartColor
    {
        get => (Color?)GetValue(GradientStartColorProperty);
        set => SetValue(GradientStartColorProperty, value);
    }

    public Color? GradientEndColor
    {
        get => (Color?)GetValue(GradientEndColorProperty);
        set => SetValue(GradientEndColorProperty, value);
    }

    public Color? ValueColor
    {
        get => (Color?)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool ShowActionIcon
    {
        get => (bool)GetValue(ShowActionIconProperty);
        set => SetValue(ShowActionIconProperty, value);
    }

    public KpiCardControl()
    {
        InitializeComponent();
        Loaded += KpiCardControl_Loaded;
        ActualThemeChanged += KpiCardControl_ActualThemeChanged;
    }

    private void KpiCardControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVisuals();
    }

    private void KpiCardControl_ActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateVisuals();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c && c.TitleText != null) c.TitleText.Text = (string)e.NewValue ?? string.Empty;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c && c.ValueText != null) c.ValueText.Text = (string)e.NewValue ?? string.Empty;
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c && c.SubtitleText != null) c.SubtitleText.Text = (string)e.NewValue ?? string.Empty;
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c && c.IconGlyph != null) c.IconGlyph.Glyph = (string)e.NewValue ?? string.Empty;
    }

    private static void OnIconBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c) c.UpdateVisuals();
    }

    private static void OnColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c) c.UpdateVisuals();
    }

    private static void OnValueColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c) c.UpdateVisuals();
    }

    private static void OnShowActionIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCardControl c && c.ActionIcon != null) c.ActionIcon.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateVisuals()
    {
        if (IconGlyph != null)
        {
            if (IconBrush != null)
            {
                IconGlyph.Foreground = IconBrush;
            }
            else if (GradientStartColor.HasValue)
            {
                IconGlyph.Foreground = new SolidColorBrush(GradientStartColor.Value);
            }
            else
            {
                // Fluent Design: Unified System Accent color for cohesive visual hierarchy
                if (Application.Current.Resources.TryGetValue("AccentTextFillColorPrimaryBrush", out var accentBrush) && accentBrush is Brush b)
                {
                    IconGlyph.Foreground = b;
                }
            }
        }

        if (ValueText != null)
        {
            if (ValueColor.HasValue)
            {
                ValueText.Foreground = new SolidColorBrush(ValueColor.Value);
            }
            else
            {
                ValueText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            }
        }
    }

    private void RootBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Fluent Design: Subtle control secondary stroke on hover instead of rainbow colors
        if (Application.Current.Resources.TryGetValue("ControlStrokeColorSecondaryBrush", out var stroke) && stroke is Brush hoverStroke)
        {
            RootBorder.BorderBrush = hoverStroke;
        }

        if (ActionIcon != null)
        {
            ActionIcon.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        }
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
        catch { }
    }

    private void RootBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        RootBorder.ClearValue(Border.BorderBrushProperty);
        if (ActionIcon != null)
        {
            ActionIcon.Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"];
        }
        try
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
        catch { }
    }

    private void RootBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }
}
