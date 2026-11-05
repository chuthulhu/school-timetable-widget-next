# ADR 0034 — Display settings window with a left section list

Status: **Accepted — user decision 2026-11-05; native verification pending**

Date: 2026-11-05

## Context

The display settings window was one long scrolling page. It held six preset buttons, five
clock checkboxes, six font groups, four opacity sliders, the theme buttons and eight color
boxes. A color and its opacity sat in different groups. The sliders showed no value. A color
input error appeared only at the bottom of the window. Presets and themes looked alike.

The user compared two interactive mock-ups, top tabs and a left list, and chose the left list.
This was stage 2 of the approved UI direction recorded in [Continuity](../CONTINUITY.md).

## Decision

- **Sections.** A left list holds four sections: 스타일, 시계, 시간표 글꼴 and 색상·투명도.
  Only the selected section is shown, and the window opens at 스타일. Switching sections only
  changes what is visible. It never previews, applies or discards anything. The Draft, Preview,
  Apply, OK, Cancel and Reset rules of ADR 0003 and ADR 0014 are unchanged, and the window-level
  error line and buttons stay visible in every section.
- **스타일.**
  - The preset card says presets store everything: fonts, clock and colors.
  - 표시 스타일 and 내 프리셋으로 저장... stay visible.
  - Rename, update, delete, export and import move into a 프리셋 관리 expander. Their enable rules
    and their behaviour are unchanged.
  - The theme card says themes change only colors and opacity.
  - Each theme button shows a strip of four colors: header background, cell background, current
    background and text.
- **시계.** The clock checkboxes, the 24-hour note, the four font editors and the size range.
- **시간표 글꼴.** The two table font editors and their size range.
- **색상·투명도.**
  - Each target is one row: label, swatch, `#RRGGBB` box and, for backgrounds and the border,
    its opacity slider with a % readout.
  - 오늘 머리글 배경 says it shares the header opacity.
  - Text rows say text is always opaque.
  - An invalid box turns red, and a message under it explains the format. The swatch keeps the
    last valid color, and Apply stays blocked as before (ADR 0027).
- No setting, value range, storage format or theme content changes. The color input stays
  text-only. The in-app color adjustment window is a later stage with its own ADR.

## Consequences

The window grows slightly to 780 × 660 (minimum 660 × 460). Font editors of a hidden section
are not laid out until their section is shown. Tests that inspect generated editors first
select the section.

## Verification

- Automated tests on unshown WPF objects cover:
  - The section list and that exactly one section is visible.
  - Switching sections preserving the Draft without saving.
  - Color rows, the slider readout and the inline error.
  - Theme strips.
  - The preset management expander and its bindings.
- Existing dialog tests select the 시계 section before reading font editors.
- Still to check natively:
  - Keyboard navigation of the section list.
  - Expander focus.
  - Rendering at 100/150/200% DPI.
  - IME input in the hex boxes.
