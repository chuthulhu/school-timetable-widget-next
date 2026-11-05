# ADR 0036 — Modern right-click menu look

Status: **Accepted — user decision 2026-11-05; native verification pending**

Date: 2026-11-05

## Context

ADR 0033 unified the widget's right-click menu, but it still used the classic WPF system
style. The user asked for the look of the approved mock-up instead.

## Decision

- An application-wide resource dictionary, `Features/WidgetMenu/WidgetMenuTheme.xaml`, merged
  in `App.xaml`, restyles WPF context menus:
  - A light `#F9F9F9` surface with an 8-DIP corner radius, a 1-DIP `#D6D6D6` border and a soft
    drop shadow.
  - Items are at least 32 DIPs tall, with a 4-DIP rounded `#E6E6E6` highlight while hovered,
    keyboard-highlighted or showing their submenu.
  - A check column draws a line check mark. Shortcut text is grey and right-aligned. A chevron
    marks submenus, which open to the right in the same rounded surface.
  - Separators are thin `#E0E0E0` lines. Disabled items are grey.
  - The font is Segoe UI Variable Text, falling back to Segoe UI and Malgun Gothic, at 14 DIPs.
- The styles are implicit, so the code-created 학기 items match. Text boxes' built-in WPF
  context menus share the look too.
- The menu structure and behaviour of ADR 0033 are unchanged, as is keyboard navigation
  through the standard `PART_Popup` submenu part. The tray menu is a Windows Forms menu and is
  unchanged.

## Consequences

The menu no longer follows the Windows high-contrast system colors automatically. That remains
a known limitation for a later accessibility pass.

## Verification

- An automated test loads the dictionary and checks the menu surface, its corner radius and
  that submenu headers keep `PART_Popup` and their chevron.
- Still to check natively:
  - Shadow and transparency on real desktops.
  - Submenu placement and keyboard navigation.
  - DPI rendering.
