using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BrowserHost.XamlUtilities;

public static class AnimationHelper
{
    public static void AnimateGridColumnExpandCollapse(ColumnDefinition gridColumn, bool expand, double expandedWidth, double collapsedWidth, double minWidth, TimeSpan duration)
    {
        if (expand && gridColumn.ActualWidth >= expandedWidth)
            // It's as wide as it needs to be.
            return;

        if (!expand && gridColumn.ActualWidth == collapsedWidth)
            // It's already collapsed.
            return;

        var storyBoard = new Storyboard();
        var animation = new GridLengthAnimation
        {
            From = new GridLength(gridColumn.ActualWidth),
            To = new GridLength(expand ? expandedWidth : collapsedWidth),
            Duration = duration
        };

        // Set delegate that will fire on completion.
        animation.Completed += delegate
        {
            // Set the animation to null on completion. This allows the grid to be resized manually
            gridColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
            // Set the final value manually.
            gridColumn.Width = new GridLength(expand ? expandedWidth : collapsedWidth);
            gridColumn.MinWidth = minWidth;
        };

        storyBoard.Children.Add(animation);

        Storyboard.SetTarget(animation, gridColumn);
        Storyboard.SetTargetProperty(animation, new PropertyPath(ColumnDefinition.WidthProperty));
        storyBoard.Children.Add(animation);

        // Begin the animation.
        storyBoard.Begin();
    }

    public static void AnimateGridRowExpandCollapse(RowDefinition gridRow, bool expand, double expandedHeight, double collapsedHeight, double minHeight, TimeSpan duration)
    {
        if (expand && gridRow.ActualHeight >= expandedHeight)
            // It's as high as it needs to be.
            return;

        if (!expand && gridRow.ActualHeight == collapsedHeight)
            // It's already collapsed.
            return;

        var storyBoard = new Storyboard();
        GridLengthAnimation animation = new GridLengthAnimation
        {
            From = new GridLength(gridRow.ActualHeight),
            To = new GridLength(expand ? expandedHeight : collapsedHeight),
            Duration = duration
        };

        // Set delegate that will fire on completion.
        animation.Completed += delegate
        {
            // Set the animation to null on completion. This allows the grid to be resized manually
            gridRow.BeginAnimation(RowDefinition.HeightProperty, null);
            // Set the final height.
            gridRow.Height = new GridLength(expand ? expandedHeight : collapsedHeight);
            gridRow.MinHeight = minHeight;
        };

        storyBoard.Children.Add(animation);

        Storyboard.SetTarget(animation, gridRow);
        Storyboard.SetTargetProperty(animation, new PropertyPath(RowDefinition.HeightProperty));
        storyBoard.Children.Add(animation);

        // Begin the animation.
        storyBoard.Begin();
    }
}
