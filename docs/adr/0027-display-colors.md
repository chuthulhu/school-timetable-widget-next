# ADR 0027 — Display colors

Status: **Accepted — user-approved scope 2026-10-26; native verification pending**

Date: 2026-10-26

## Context

Product Contract M5 requires live preview, Apply persistence and restart restore for font,
color and opacity. ADR 0026 delivered table typography and brush opacity. Every color stayed
fixed in code. The 2026-10-19 laptop check found black text hard to read over dark desktop
icons, seen through transparent cells. On 2026-10-26 the user chose freely editable colors,
including text colors. They deferred themes. They chose seven table colors plus one
clock-area text color, and RGB-only colors whose transparency stays in the existing opacity
sliders.

## Decision

- `DisplayColors` holds eight opaque 24-bit RGB values:
  - Table backgrounds: header, today's header, ordinary cell and current period.
  - Table border.
  - Text: table header, table cell, and the clock area. The clock area covers time,
    AM/PM, date, weekday and status.
- Defaults reproduce the prior look with fixed values: `#F0F0F0`, `#E5EFF6`, `#FFFFFF`,
  `#FFF3CD`, `#E3E3E3`, and black text. The prior header and border used Windows system
  colors. Fixed defaults keep presets and backups portable. As a result, those two colors no
  longer follow a changed system palette. The weekday line under each date header now uses
  the header text color instead of a separate fixed gray. Week-arrow glyphs and the cell
  focus outline follow the header and cell text colors, so they stay readable on custom
  backgrounds.
- Colors carry no alpha. Background and border brushes keep ADR 0026 opacity. Text brushes
  are always fully opaque. This satisfies R11 by offering no alpha input.
- Colors are part of the display configuration. They therefore follow the existing paths:
  - Draft, Preview, Apply, OK, Cancel and Reset.
  - User presets and `.stwpreset` export/import.
  - Profile save and full backup/restore.
- Storage adds an optional strict `colors` object next to `table` in every display payload
  (profile v5 display and user presets, preset file v1, embedded backup profile). Its fields
  are `version: 1` plus eight `#RRGGBB` strings. Readers accept either hex case, and writers
  emit uppercase.
- An absent `colors` object means defaults. A null, incomplete, unknown-field, unknown-version
  or malformed object rejects the whole candidate. A profile that fails this way loads with
  writes blocked and its bytes preserved. Older strict readers reject the extension instead of
  silently dropping it.
- Before the first write that adds `colors` to a profile that already has `table` in every
  payload, the store flushes the exact original bytes to `profile-before-colors-*.json`. A
  profile that also lacks `table` keeps the single ADR 0026 `profile-before-table-*.json`
  backup. Loading alone never rewrites.
- The settings dialog shows one `#RRGGBB` text box and a swatch per color. Invalid text shows
  an error naming the color and keeps the last valid preview. It blocks Apply, like other
  invalid display input. The swatch shows the last valid value.

## Consequences

Profile v5, preset file v1 and backup v1 remain the outer versions. No schema version bump is
needed because the extension is optional and strictly validated. Themes, named palettes and a
graphical color picker remain future work. Legacy `style_settings.json` colors (C2) can map
losslessly onto RGB values, but migration remains a separate PLANNED feature.

## Verification

- Automated tests cover:
  - RGB parsing.
  - Default parity.
  - Profile, preset and backup round trips.
  - Invalid and future objects blocking writes.
  - The pre-colors byte backup.
  - Draft Preview, Apply and Cancel.
  - Brush, text and clock application on unshown WPF objects.
- These are not native rendering or readability evidence. Native Windows checks remain
  required: contrast over real desktops, the dialog at 100/150/200% DPI, and keyboard/IME
  input in the hex boxes.
