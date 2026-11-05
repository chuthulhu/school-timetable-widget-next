# ADR 0030 — Rounded table tiles and right-click semester menu

Status: **Accepted — user request 2026-11-03; native verification pending**

Date: 2026-11-03

## Context

The user compared the widget with their earlier app in a screenshot. They asked for three
changes and one more:

- More space between cells.
- Rounder cell corners.
- Semester selection moved out of the widget body into a menu, because the visible
  combo box and button looked out of place.
- No white line under the clock area.

## Decision

- **Tiles.** Every table slot is a separate tile: ordinary cells, date and period headers,
  and the corner. Each tile has a 2-DIP margin, giving a 4-DIP gap between neighbours. It
  has a 6-DIP corner radius and a 1-DIP border on all sides in the ADR 0027 border color
  and ADR 0026 border opacity. The keyboard focus outline follows the rounded tile. The
  current-period highlight still changes only the background, so geometry stays identical
  with or without a highlight. Gaps lie on the table's existing 1/255 hit surface and keep
  ADR 0026 input behaviour.
- **Header line.** The clock area no longer draws a bottom border line.
- **Semester menu.** Both right-click menus, the window's and the timetable's, gain a 학기
  submenu after the window items. It lists every semester, with a check on the active one,
  then 학기 관리.... Choosing a semester runs the same ADR 0022 transaction as the former
  selector. It is refused while an editor window is open. After success or failure the
  list is rebuilt from committed state, so a failed switch never leaves the candidate
  checked. Underscores in names are shown literally. The widget body no longer shows the
  semester controls.

Amended by [ADR 0033](0033-unified-widget-context-menu.md): the two right-click menus are now one
shared menu, and the 학기 submenu leads it. The semester behaviour above is unchanged.

## Consequences

The active semester name is no longer visible on the widget face. It is shown by the check
in the 학기 submenu and in the management dialog. Geometry values are fixed in the view
template, not user settings.

## Verification

- Automated: existing highlight and geometry invariance tests, and semester menu tests for
  the committed check, failed switches, successful switches and the management item.
- Not verified: native rendering of the rounded tiles, the gaps over real desktops, and
  menu interaction.
