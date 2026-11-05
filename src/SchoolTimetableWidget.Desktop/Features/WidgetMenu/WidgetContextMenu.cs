using System.Windows;
using System.Windows.Controls;

namespace SchoolTimetableWidget.Desktop.Features.WidgetMenu;

/// <summary>
/// Helpers for the widget's single right-click menu (ADR 0033). The menu content lives in the
/// timetable view's XAML; features only reach their own items through these helpers.
/// </summary>
public static class WidgetContextMenu
{
    /// <summary>Every menu item, including those inside submenus, in display order.</summary>
    public static IEnumerable<MenuItem> AllItems(ItemsControl menu)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            yield return item;
            foreach (var child in AllItems(item)) yield return child;
        }
    }

    /// <summary>Sets one fixed command target for every command item, whichever element opened the menu.</summary>
    public static void SetCommandTargets(ItemsControl menu, IInputElement target)
    {
        foreach (var item in AllItems(menu).Where(item => item.Command is not null)) item.CommandTarget = target;
    }
}
