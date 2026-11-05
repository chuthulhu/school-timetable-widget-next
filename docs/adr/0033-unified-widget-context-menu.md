# ADR 0033 — Unified widget right-click menu

Status: **Accepted — user decision 2026-11-04/05; native verification pending**

Date: 2026-11-05

## Context

Before this change the widget had two right-click menus. Right-clicking the clock area showed
a short window menu: display settings, window settings, window reset, 학기, backup and restore.
Right-clicking the timetable showed a longer flat menu that also held import, period editing,
date overrides and the lunch option. Its seven separators grouped little. The 학기 submenu was
inserted by item index, so moving a XAML item could misplace it. Hiding the widget and its
movement lock were only reachable from the tray menu and the 창 설정 dialog.

The user reviewed an interactive mock-up and chose the recommended structure. They added a
위젯 숨기기 item and asked for 위치 고정 and 위치 고정 풀기 items.

## Decision

- **One menu.** The clock area and the timetable open the same menu instance. It is defined
  once in the timetable view and shared by MainWindow. Every command item targets the
  timetable view, whichever element opened the menu. Window-level commands bubble from the
  view to MainWindow, so they work from both areas.
- **Structure.**

  ```
  학기 ▸ (semesters, 학기 관리...)        ← only when a profile runtime exists
  ─────
  시간표 ▸ 학교 시간표 가져오기... (Ctrl+V) / 표준 양식 가져오기... / ─ / 표준 양식 복사
  일과 ▸ 일과 시간 편집... / 날짜별 예외 설정... / ─ / 점심시간 표시 (4·5교시 사이)
  ─────
  표시 설정... (Ctrl+,)
  창 ▸ 창 설정... / 창 위치·크기 초기화
  데이터 ▸ 백업... / 복원...
  ─────
  위치 고정  |  위치 고정 풀기
  위젯 숨기기
  ```

  The 학기 submenu is inserted at the top, by position zero, not by a middle index. Its
  ADR 0030 behaviour is unchanged.
- **위치 고정.** The item toggles the existing ADR 0019 movement lock. Resizing stays
  allowed. It is saved at once, like 창 위치·크기 초기화. It is not part of the display
  settings Draft and Cancel does not undo it. The label names the available action: 위치 고정
  while unlocked and 위치 고정 풀기 while locked. It follows the committed state. It also
  updates after the 창 설정 dialog changes the lock. While a native move or resize is in
  progress the item is disabled. If the save fails, the previous lock stays in force, the label
  is unchanged and a warning says the setting was not saved.
- **위젯 숨기기.** The item uses the same close-to-tray path as X and Alt+F4 (ADR 0020).
  That includes the one-time tray notice and the open-editor guard. 종료 stays in the tray
  menu only, so it cannot be chosen by accident from the widget.
- **Recovery Required.** MainWindow disables the timetable while recovery is required. A
  disabled timetable offers no timetable actions from the shared menu: import, template copy,
  period editing, date overrides and the lunch option. Window, backup and restore entries keep
  their existing rules.
- **Cell tooltip.** The cell tooltip is shortened to 더블클릭 또는 F2로 편집. The menu itself
  now shows where import and schedule actions live.

The menu look is restyled by [ADR 0036](0036-modern-context-menu-theme.md); its structure is unchanged.

## Consequences

- Menu labels changed: 창 위치/크기 초기화 is now 창 위치·크기 초기화. 데이터 백업... and
  데이터 복원... are now 백업... and 복원... under 데이터. Commands and their behaviour are
  unchanged.
- The tray menu is unchanged.
- No profile, preset, backup or window-state format changes.

## Verification

- Automated tests cover the following on unshown WPF objects:
  - One shared menu, its exact grouping, and the fixed command target.
  - The 학기 submenu at the top, inserted once.
  - Timetable actions disabled in a disabled view.
  - The hide binding.
  - Lock toggling with the saved state and the label.
  - A failed lock save, and a lock change made from 창 설정.
- Existing menu tests were updated to search inside submenus.
- These are not native evidence. Still to be checked on Windows:
  - Right-click from both areas.
  - Submenu keyboard navigation.
  - Hide and the tray notice.
  - Lock while dragging.
  - Menu rendering at 100/150/200% DPI.
