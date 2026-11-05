# ADR 0037 — User-arranged clock area

Status: **Accepted — user decision 2026-11-05; native verification pending**

Date: 2026-11-05

## Context

Each built-in display style had a fixed clock-area arrangement:

- 표준: one line of date and weekday, time, then status.
- 컴팩트 and 미니멀: one line of time, date and weekday, then status.
- 디지털: three centered lines — time, then date and weekday, then status.

Date and weekday always stayed together. The user asked to arrange each element themselves:
left, center or right position, and order. This is stage 4 of the approved UI direction in
[Continuity](../CONTINUITY.md). When asked, the user chose:

- Up to three lines, so 디지털 keeps its look.
- The style supplies the starting arrangement, and the user edits it.
- Date and weekday can be placed separately.

## Decision

- **Model.**
  - A display configuration has a clock layout: four slots — date, weekday, time and status.
  - Each slot has a line from 1 to 3 and a position: left, center or right.
  - Every element appears exactly once. List order is the order within the same line and
    position.
  - Visibility stays with the existing show-date, show-weekday and show-status options. The
    time is always shown.
- **Styles.** Each built-in style starts with exactly its former arrangement. Choosing a style,
  or 기본값으로, loads that style's arrangement into the Draft. User presets, preset files,
  the profile and backups store the edited arrangement with the other display settings.
- **Editing.** The 시계 section of display settings (ADR 0034) gains a 배치 card. Each element
  has a row with a line choice (1줄, 2줄, 3줄), a position choice (왼쪽, 가운데, 오른쪽) and ▲/▼
  to change the order. Every change previews at once. The Draft, Apply and Cancel rules are
  unchanged.
- **Placement.**
  - The clock area has one row per line up to the highest line used. Each row has left, center
    and right cells.
  - A row with a centered element keeps it truly centered between two equal sides. Otherwise
    the side holding the status, or the left side, takes the free width.
  - Element gaps: 16 DIPs, or 8 DIPs between a date followed by its weekday.
  - Text alignment follows the position.
  - Widths of the date, weekday and time stay reserved as before, so clock ticks never move
    the layout.
  - Only the status shrinks, with an ellipsis, when it is not centered; a centered status keeps
    its reserved width. The widget's minimum width keeps the time, date and weekday whole, so
    the time is never truncated.
  - Hidden elements take no space.
  - Height:
    - One line keeps the former spacing: compact styles 28 DIPs, others 40.
    - Several lines add 8 DIPs after line 1 and 6 after each later line, plus 48.
- **Storage.**
  - Every display payload gains an optional strict object: `clock` with `version: 1` and
    `elements` — four `{element, line, position}` entries with canonical names such as `Time`
    and `Center`. This covers the profile display and user presets, preset files v1 and the
    embedded backup profile.
  - An absent object means the arrangement of the stored `layout`, so older files keep their
    look.
  - A null, incomplete, duplicate, out-of-range, unknown-field, unknown-version or misspelled
    object rejects the whole candidate. A profile that fails this way loads with writes blocked
    and its bytes preserved.
  - Before the first write that adds `clock` to a profile that already has `table` and `colors`
    in every payload, the store flushes the exact original bytes to `profile-before-clock-*.json`.
    An older profile keeps the existing single table or colors backup.
  - The stored `layout` value stays as the style's spacing family and the fallback when `clock`
    is absent.
- No profile, preset file or backup version bump.

## Consequences

Older strict app versions reject the new `clock` object instead of silently dropping it, as
with ADR 0026 and ADR 0027. A display configuration without an explicit clock layout equals the
same configuration with its style's layout stored explicitly.

## Verification

- Automated tests cover:
  - Style arrangements and equality of derived and explicit layouts.
  - Validation, and round trips through the profile, preset file and backup.
  - Absent `clock` in profiles and preset files.
  - Invalid and future objects blocking writes, and the exact pre-clock backup.
  - Draft editing, reordering, Apply, style switching and Reset.
  - Header placement by line, position and order, stable geometry across ticks, and hidden
    elements staying placed.
- Existing header and display tests still cover every style's row count and height relation.
- Not native evidence. Still to check on Windows:
  - Rendering and centering on real desktops.
  - Narrow windows.
  - Rendering at 100/150/200% DPI.
  - Keyboard use of the 배치 card.
