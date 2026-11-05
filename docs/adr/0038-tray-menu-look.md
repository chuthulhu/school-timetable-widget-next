# ADR 0038 — Tray menu matches the widget menu look

Status: **Accepted — user decision 2026-11-05; native verification pending**

Date: 2026-11-05

## Context

ADR 0036 restyled the widget's WPF right-click menus but left the tray menu alone. The tray
menu is a Windows Forms `ContextMenuStrip` and kept the classic system look. As a follow-up the
user chose to make the tray menu match.

## Decision

- The tray menu keeps its Windows Forms implementation, its items, their order and their
  behaviour (ADR 0020, ADR 0021, ADR 0032).
- A custom renderer draws it with the colors of ADR 0036:
  - A `#F9F9F9` surface with a `#D6D6D6` border.
  - A rounded `#E6E6E6` highlight on the hovered item and a thin `#E0E0E0` separator line.
  - A line check mark for 「Windows 시작 시 실행」.
  - `#1A1A1A` text, and `#9A9A9A` for disabled items.
  - There is no grey image strip.
- The font is Segoe UI at 10.5 pt (14 DIPs). Vertical item padding brings items to about the
  same height as the widget menu, and item widths still follow their text.
- On Windows 11 the menu asks the system for rounded corners
  (`DWMWA_WINDOW_CORNER_PREFERENCE`). Windows 10 ignores the request and keeps square corners.
  A failed request is ignored.

## Consequences

The two menus look alike without moving the tray menu to WPF, which would bring tray focus and
foreground handling risks. Like ADR 0036, the drawn colors do not follow Windows high-contrast
themes.

## Verification

- Automated tests use unpublished Forms objects and an in-memory bitmap. They cover:
  - The renderer, font, margins, item padding, unchanged items and automatic widths.
  - The painted surface and border.
  - The rounded highlight path.
- Still to check natively:
  - The menu opened from the real tray, hover and keyboard navigation.
  - Windows 11 rounding and Windows 10 square corners.
  - DPI rendering.
