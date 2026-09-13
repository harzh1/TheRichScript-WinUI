using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace TheRichScript_WinUI.Helpers;

/// <summary>
/// Helper and attached behavior to enforce strict numeric-only input on NumberBox controls.
/// Blocks non-numeric keys (letters, symbols, spaces) on PreviewKeyDown and validates pasted/IME text via BeforeTextChanging.
/// </summary>
public static class NumericInputHelper
{
    public static readonly DependencyProperty IsNumericOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsNumericOnly",
            typeof(bool),
            typeof(NumericInputHelper),
            new PropertyMetadata(false, OnIsNumericOnlyChanged));

    public static bool GetIsNumericOnly(DependencyObject obj) => (bool)obj.GetValue(IsNumericOnlyProperty);
    public static void SetIsNumericOnly(DependencyObject obj, bool value) => obj.SetValue(IsNumericOnlyProperty, value);

    public static readonly DependencyProperty AllowDecimalsProperty =
        DependencyProperty.RegisterAttached(
            "AllowDecimals",
            typeof(bool),
            typeof(NumericInputHelper),
            new PropertyMetadata(true));

    public static bool GetAllowDecimals(DependencyObject obj) => (bool)obj.GetValue(AllowDecimalsProperty);
    public static void SetAllowDecimals(DependencyObject obj, bool value) => obj.SetValue(AllowDecimalsProperty, value);

    public static readonly DependencyProperty AllowNegativeProperty =
        DependencyProperty.RegisterAttached(
            "AllowNegative",
            typeof(bool),
            typeof(NumericInputHelper),
            new PropertyMetadata(false));

    public static bool GetAllowNegative(DependencyObject obj) => (bool)obj.GetValue(AllowNegativeProperty);
    public static void SetAllowNegative(DependencyObject obj, bool value) => obj.SetValue(AllowNegativeProperty, value);

    private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox && e.NewValue is true)
        {
            var allowDecimals = GetAllowDecimals(numberBox);
            var allowNegative = GetAllowNegative(numberBox);
            Attach(numberBox, allowDecimals, allowNegative);
        }
    }

    /// <summary>
    /// Attaches keyboard and text-changing filters to the specified NumberBox.
    /// </summary>
    public static void Attach(NumberBox numberBox, bool allowDecimals = true, bool allowNegative = false)
    {
        if (numberBox == null) return;

        numberBox.AcceptsExpression = false;

        // 1. Intercept keystrokes at the control tunneling level
        numberBox.PreviewKeyDown -= NumberBox_PreviewKeyDown;
        numberBox.PreviewKeyDown += NumberBox_PreviewKeyDown;

        // 2. Attach BeforeTextChanging on the internal TextBox once loaded
        void HookInternalTextBox()
        {
            var textBox = FindVisualChild<TextBox>(numberBox);
            if (textBox != null)
            {
                textBox.BeforeTextChanging -= TextBox_BeforeTextChanging;
                textBox.BeforeTextChanging += TextBox_BeforeTextChanging;
            }
        }

        if (numberBox.IsLoaded)
        {
            HookInternalTextBox();
        }
        else
        {
            RoutedEventHandler? loadedHandler = null;
            loadedHandler = (s, e) =>
            {
                numberBox.Loaded -= loadedHandler;
                HookInternalTextBox();
            };
            numberBox.Loaded += loadedHandler;
        }

        // Store configuration in attached properties
        SetAllowDecimals(numberBox, allowDecimals);
        SetAllowNegative(numberBox, allowNegative);
    }

    private static void NumberBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not NumberBox numberBox) return;

        bool allowDecimals = GetAllowDecimals(numberBox);
        bool allowNegative = GetAllowNegative(numberBox);

        // Allow modifier shortcuts (e.g. Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+X, Ctrl+Z)
        var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
        {
            return;
        }

        var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
        {
            return;
        }

        // Allow navigation and editing keys
        if (e.Key is VirtualKey.Back or VirtualKey.Delete or VirtualKey.Tab or
            VirtualKey.Enter or VirtualKey.Escape or VirtualKey.Left or VirtualKey.Right or
            VirtualKey.Up or VirtualKey.Down or VirtualKey.Home or VirtualKey.End)
        {
            return;
        }

        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

        // Digits on top number row (must NOT have shift pressed, otherwise it's !, @, #, etc.)
        if (!isShiftDown && e.Key >= VirtualKey.Number0 && e.Key <= VirtualKey.Number9)
        {
            return;
        }

        // Digits on numpad
        if (e.Key >= VirtualKey.NumberPad0 && e.Key <= VirtualKey.NumberPad9)
        {
            return;
        }

        // Decimal separator
        if (allowDecimals)
        {
            if (e.Key == VirtualKey.Decimal) return;
            // VirtualKey 190 is OEM period ('.'), 188 is OEM comma (',')
            if (!isShiftDown && ((int)e.Key == 190 || (int)e.Key == 188))
            {
                return;
            }
        }

        // Negative sign
        if (allowNegative && !isShiftDown && ((int)e.Key == 189 || e.Key == VirtualKey.Subtract))
        {
            return;
        }

        // All other keys (letters A-Z, spaces, symbols like Shift+digit, punctuation) are blocked
        e.Handled = true;
    }

    private static void TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Allow clearing the text box
        if (string.IsNullOrEmpty(args.NewText)) return;

        // Find parent NumberBox to get config
        var numberBox = FindVisualParent<NumberBox>(sender);
        bool allowDecimals = numberBox != null ? GetAllowDecimals(numberBox) : true;
        bool allowNegative = numberBox != null ? GetAllowNegative(numberBox) : false;

        if (!IsValidNumericInput(args.NewText, allowDecimals, allowNegative))
        {
            args.Cancel = true;
        }
    }

    /// <summary>
    /// Checks if a prospective string matches valid numeric input while being typed or pasted.
    /// </summary>
    public static bool IsValidNumericInput(string text, bool allowDecimals, bool allowNegative)
    {
        if (string.IsNullOrEmpty(text)) return true;

        if (allowNegative && text == "-") return true;
        if (allowDecimals && (text == "." || text == ",")) return true;

        int decimalCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (i == 0 && c == '-' && allowNegative) continue;

            if ((c == '.' || c == ',') && allowDecimals)
            {
                decimalCount++;
                if (decimalCount > 1) return false;
                continue;
            }

            if (!char.IsDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var nested = FindVisualChild<T>(child);
            if (nested != null) return nested;
        }
        return null;
    }

    public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typedParent) return typedParent;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
