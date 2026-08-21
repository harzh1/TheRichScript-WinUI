using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace TheRichScript_WinUI.Controls;

public class UniformGridPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(UniformGridPanel),
            new PropertyMetadata(0, (d, e) => ((UniformGridPanel)d).InvalidateMeasure()));

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(
            nameof(ColumnSpacing),
            typeof(double),
            typeof(UniformGridPanel),
            new PropertyMetadata(0.0, (d, e) => ((UniformGridPanel)d).InvalidateMeasure()));

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = Children.Count;
        if (count == 0) return new Size(0, 0);

        int cols = Columns > 0 ? Columns : count;
        double spacingTotal = Math.Max(0, (cols - 1) * ColumnSpacing);
        double colWidth = Math.Max(0, (availableSize.Width - spacingTotal) / cols);
        double maxHeight = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(colWidth, availableSize.Height));
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        double totalWidth = double.IsInfinity(availableSize.Width) ? (colWidth * cols + spacingTotal) : availableSize.Width;
        double totalHeight = double.IsInfinity(availableSize.Height) ? maxHeight : availableSize.Height;

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = Children.Count;
        if (count == 0) return finalSize;

        int cols = Columns > 0 ? Columns : count;
        double spacingTotal = Math.Max(0, (cols - 1) * ColumnSpacing);
        double colWidth = Math.Max(0, (finalSize.Width - spacingTotal) / cols);

        for (int i = 0; i < count; i++)
        {
            var child = Children[i];
            int colIndex = i % cols;
            double x = colIndex * (colWidth + ColumnSpacing);
            child.Arrange(new Rect(x, 0, colWidth, finalSize.Height));
        }

        return finalSize;
    }
}
