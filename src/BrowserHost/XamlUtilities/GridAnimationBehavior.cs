using System;
using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.XamlUtilities;

public class GridAnimationBehavior : DependencyObject
{
    #region Attached IsExpanded DependencyProperty

    /// <summary>
    /// Register the "IsExpanded" attached property and the "OnIsExpanded" callback 
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
      DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(GridAnimationBehavior),
        new FrameworkPropertyMetadata(OnIsExpandedChanged));

    public static void SetIsExpanded(DependencyObject dependencyObject, bool value)
    {
        dependencyObject.SetValue(IsExpandedProperty, value);
    }

    #endregion

    #region Attached Duration DependencyProperty

    /// <summary>
    /// Register the "Duration" attached property 
    /// </summary>
    public static readonly DependencyProperty DurationProperty =
      DependencyProperty.RegisterAttached("Duration", typeof(TimeSpan), typeof(GridAnimationBehavior),
        new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(200)));

    public static void SetDuration(DependencyObject dependencyObject, TimeSpan value)
    {
        dependencyObject.SetValue(DurationProperty, value);
    }

    private static TimeSpan GetDuration(DependencyObject dependencyObject)
    {
        return (TimeSpan)dependencyObject.GetValue(DurationProperty);
    }

    #endregion

    #region GridCellSize DependencyProperty

    /// <summary>
    /// Use a private "GridCellSize" dependency property as a temporary backing 
    /// store for the last expanded grid cell size (row height or column width).
    /// </summary>
    private static readonly DependencyProperty GridCellSizeProperty =
      DependencyProperty.Register("GridCellSize", typeof(double), typeof(GridAnimationBehavior),
        new UIPropertyMetadata(0.0));

    private static void SetGridCellSize(DependencyObject dependencyObject, double value)
    {
        dependencyObject.SetValue(GridCellSizeProperty, value);
    }

    private static double GetGridCellSize(DependencyObject dependencyObject)
    {
        return (double)dependencyObject.GetValue(GridCellSizeProperty);
    }

    #endregion

    /// <summary>
    /// Called when the attached <c>IsExpanded</c> property changed.
    /// </summary>
    private static void OnIsExpandedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var duration = GetDuration(dependencyObject);
        if (dependencyObject is RowDefinition rowDefinition)
        {
            // The IsExpanded attached property of a RowDefinition changed
            if ((bool)e.NewValue)
            {
                var expandedHeight = GetGridCellSize(rowDefinition);
                if (expandedHeight > 0)
                {
                    // Animate row height back to saved expanded height.
                    AnimationHelper.AnimateGridRowExpandCollapse(rowDefinition, true, expandedHeight, rowDefinition.ActualHeight, 0, duration);
                }
            }
            else
            {
                // Save expanded height and animate row height down to zero.
                SetGridCellSize(rowDefinition, rowDefinition.ActualHeight);
                AnimationHelper.AnimateGridRowExpandCollapse(rowDefinition, false, rowDefinition.ActualHeight, 0, 0, duration);
            }
        }

        if (dependencyObject is ColumnDefinition columnDefinition)
        {
            // The IsExpanded attached property of a ColumnDefinition changed
            if ((bool)e.NewValue)
            {
                var expandedWidth = GetGridCellSize(columnDefinition);
                if (expandedWidth > 0)
                {
                    // Animate column width back to saved expanded width.
                    AnimationHelper.AnimateGridColumnExpandCollapse(columnDefinition, true, expandedWidth, columnDefinition.ActualWidth, 0, duration);
                }
            }
            else
            {
                // Save expanded width and animate column width down to zero.
                SetGridCellSize(columnDefinition, columnDefinition.ActualWidth);
                AnimationHelper.AnimateGridColumnExpandCollapse(columnDefinition, false, columnDefinition.ActualWidth, 0, 0, duration);
            }
        }
    }
}