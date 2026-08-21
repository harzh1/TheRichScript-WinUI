using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace TheRichScript_WinUI.Controls;

public class StaggeredPanel : Panel
{
    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(nameof(ColumnSpacing), typeof(double), typeof(StaggeredPanel), new PropertyMetadata(16.0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty RowSpacingProperty =
        DependencyProperty.Register(nameof(RowSpacing), typeof(double), typeof(StaggeredPanel), new PropertyMetadata(16.0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty DesiredColumnWidthProperty =
        DependencyProperty.Register(nameof(DesiredColumnWidth), typeof(double), typeof(StaggeredPanel), new PropertyMetadata(440.0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty MaxColumnsProperty =
        DependencyProperty.Register(nameof(MaxColumns), typeof(int), typeof(StaggeredPanel), new PropertyMetadata(2, OnLayoutPropertyChanged));

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public double DesiredColumnWidth
    {
        get => (double)GetValue(DesiredColumnWidthProperty);
        set => SetValue(DesiredColumnWidthProperty, value);
    }

    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StaggeredPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double availableWidth = availableSize.Width;
        if (double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            availableWidth = 800;
        }

        int numColumns = Math.Max(1, (int)Math.Floor((availableWidth + ColumnSpacing) / (DesiredColumnWidth + ColumnSpacing)));
        if (MaxColumns > 0)
        {
            numColumns = Math.Min(numColumns, MaxColumns);
        }

        double columnWidth = (availableWidth - (numColumns - 1) * ColumnSpacing) / numColumns;
        double[] columnHeights = new double[numColumns];

        foreach (UIElement child in Children)
        {
            if (child.Visibility == Visibility.Collapsed) continue;

            child.Measure(new Size(columnWidth, double.PositiveInfinity));

            int shortestCol = 0;
            for (int i = 1; i < numColumns; i++)
            {
                if (columnHeights[i] < columnHeights[shortestCol])
                {
                    shortestCol = i;
                }
            }

            if (columnHeights[shortestCol] > 0)
            {
                columnHeights[shortestCol] += RowSpacing;
            }
            columnHeights[shortestCol] += child.DesiredSize.Height;
        }

        double totalHeight = 0;
        for (int i = 0; i < numColumns; i++)
        {
            if (columnHeights[i] > totalHeight)
            {
                totalHeight = columnHeights[i];
            }
        }

        return new Size(availableWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double width = finalSize.Width;
        int numColumns = Math.Max(1, (int)Math.Floor((width + ColumnSpacing) / (DesiredColumnWidth + ColumnSpacing)));
        if (MaxColumns > 0)
        {
            numColumns = Math.Min(numColumns, MaxColumns);
        }

        double columnWidth = (width - (numColumns - 1) * ColumnSpacing) / numColumns;
        double[] columnHeights = new double[numColumns];

        foreach (UIElement child in Children)
        {
            if (child.Visibility == Visibility.Collapsed) continue;

            int shortestCol = 0;
            for (int i = 1; i < numColumns; i++)
            {
                if (columnHeights[i] < columnHeights[shortestCol])
                {
                    shortestCol = i;
                }
            }

            double x = shortestCol * (columnWidth + ColumnSpacing);
            double y = columnHeights[shortestCol];
            double childHeight = child.DesiredSize.Height;

            child.Arrange(new Rect(x, y, columnWidth, childHeight));

            columnHeights[shortestCol] += childHeight + RowSpacing;
        }

        double totalHeight = 0;
        for (int i = 0; i < numColumns; i++)
        {
            if (columnHeights[i] > totalHeight)
            {
                totalHeight = columnHeights[i];
            }
        }

        return new Size(width, totalHeight);
    }
}
