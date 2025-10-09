using System.Windows;
using System.Windows.Media;

namespace UAssetManager.Utils;
internal static class VisualUtils
{
    /// <summary>
    /// Sets the visibility of the specified <see cref="UIElement"/> based on the provided value.
    /// </summary>
    /// <param name="element">The <see cref="UIElement"/> whose visibility is to be set. Cannot be <see langword="null"/>.</param>
    /// <param name="visual">A value indicating whether the element should be visible.  <see langword="true"/> to make the element visible;
    /// otherwise, <see langword="false"/> to collapse it.</param>
    public static void SetVisual(this UIElement element, bool visual)
    {
        element.Visibility = visual ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Searches the visual tree upward from the specified child element to find the first parent of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the visual parent to search for. Must derive from <see cref="DependencyObject"/>.</typeparam>
    /// <param name="child">The starting element in the visual tree. Cannot be <see langword="null"/>.</param>
    /// <returns>The first parent of type <typeparamref name="T"/> found in the visual tree, or <see langword="null"/> if no such
    /// parent exists.</returns>
    public static T? FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = child;

        while (parentObject != null && parentObject is not T)
        {
            parentObject = VisualTreeHelper.GetParent(parentObject);
        }

        return parentObject as T;
    }
}