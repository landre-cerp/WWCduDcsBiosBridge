using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWCduDcsBiosBridge.UI;

/// <summary>
/// Helper methods for UI operations
/// </summary>
public static class UIHelpers
{
    /// <summary>
    /// Finds a visual child element by its tag
    /// </summary>
    public static T? FindVisualChild<T>(DependencyObject parent, string tag) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T result && child is FrameworkElement element && element.Tag?.ToString() == tag)
            {
                return result;
            }

            var descendant = FindVisualChild<T>(child, tag);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    /// <summary>
    /// Recursively enables or disables all child controls
    /// </summary>
    public static void SetChildControlsEnabled(DependencyObject parent, bool enabled)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is Control control)
            {
                control.IsEnabled = enabled;
            }

            SetChildControlsEnabled(child, enabled);
        }
    }
}