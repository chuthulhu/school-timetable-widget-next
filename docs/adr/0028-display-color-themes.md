# ADR 0028 — Built-in display color themes

Status: **Accepted — user-approved scope 2026-10-30; native verification pending**

Date: 2026-10-30

## Context

ADR 0027 made eight colors editable and deferred themes. On 2026-10-30 the user asked for
themes based on two sources. The first is the colors they set themselves in the legacy app,
not its built-in presets. The second is the web-app design colors in their guideline
repository. The user decided four things:

- Themes load values into the Draft. No theme identity is linked or stored.
- The legacy custom values are the `src/settings/style_settings.json` in Golden Reference
  `84de32a555633120bd6363a609a19cbc0a15e8ea`. The user confirmed these are their own settings.
- Both guideline palettes ship, each with a light and a dark theme.
- No new display preset is added. Presets keep layout and fonts. Users save combinations as
  their own presets.

## Decision

Six built-in themes are offered. Each one sets the eight ADR 0027 colors and the four ADR 0026
brush opacities.

| Theme | Source | Opacities (header/cell/current/border) |
| --- | --- | --- |
| 기본 | ADR 0027 defaults and ADR 0026 default opacities | 1 / 0 / 1 / 1 |
| 이전 앱 내 설정 | Legacy `style_settings.json` at the Golden Reference | 102/255 / 51/255 / 130/255 / 7/255 |
| 슬레이트 밝음·어두움 | Guideline `slate` palette | 1 / 1 / 1 / 1 |
| 파랑 밝음·어두움 | Guideline `blue` palette | 1 / 1 / 1 / 1 |

- **Legacy mapping.** The legacy app had no separate today-header color, so today's header
  reuses the header background. It had no separate clock text color, so the clock uses the
  legacy text color (white). Legacy 0–255 opacity bytes convert losslessly as n/255. The
  legacy font (`페이퍼로지 6 SemiBold`) is not part of a theme.
- **Guideline mapping.** The source is `chuthulhu/physichu-platform` at `cfb1712`,
  `guides/_core/tokens.json` (`_core@1.4.1-draft`). The semantic names map as follows:

  | Widget slot | Light | Dark |
  | --- | --- | --- |
  | Header background | soft | soft |
  | Today header background | accent-soft | accent-soft |
  | Cell background | paper | paper |
  | Current period | `b-300` | `b-600` |
  | Border | line | line |
  | Header, cell and clock text | ink | ink |

  The current period uses a stronger accent step, not accent-soft. With accent-soft the
  slate-light current cell was only 1.17:1 against ordinary cells. The guideline palettes are
  flat and contrast-checked when opaque, so their themes set every opacity to 1. Every
  table-text pair measures at least 4.5:1 (lowest 4.86:1).
- **Values are snapshots.** Hex values are copied into `DisplayThemes`. They are never read
  from the guideline repository at runtime. Later guideline changes need an explicit update.
- **Loading a theme** sets the eight color drafts and the four opacity sliders, then previews.
  The existing transaction applies: nothing is saved until Apply/OK, and Cancel restores the
  last applied state. Every value stays editable afterwards.
- **No storage change.** Profile, preset files and backups store only the resulting values,
  never a theme ID.

## Consequences and limits

The clock area has no background. A dark theme's light clock text over a light desktop, or a
light theme's dark text over a dark desktop, can be hard to read. Users can change the clock
text color. A clock-area background is not part of this decision. Linked themes, user-defined
themes and a graphical picker remain future work. The legacy theme reproduces the legacy
colors exactly. Its white cell text on the gold current period is not contrast-adjusted.

## Verification

- Automated tests cover:
  - The catalog: order, unique IDs, valid opacities.
  - The default and legacy values.
  - Guideline opacity and table-text contrast of at least 4.5:1.
  - Loading a theme: Draft, Preview and Cancel.
  - Editing after loading, Apply, and the absence of a stored theme field.
- Native Windows checks remain: real-desktop readability, dialog layout at 100/150/200% DPI.

Applied guidelines: `_core@1.4.1-draft` palette values only (tokens.json). The `webapp-design`
DESIGN.md is a draft placeholder and was not applied.
