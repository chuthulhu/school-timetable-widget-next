# ADR 0035 — In-app color adjustment window

Status: **Accepted — user decision 2026-11-05; native verification pending**

Date: 2026-11-05

## Context

ADR 0027 left colors as `#RRGGBB` text with a swatch and deferred a graphical picker. A
teacher who does not know hex codes could not choose a color outside the themes. The user
reviewed interactive mock-ups. They asked for a color adjustment window that opens when a
color swatch is clicked, inside the app rather than the Windows color dialog. This is stage 3
of the approved UI direction in [Continuity](../CONTINUITY.md).

## Decision

- **Opening.** Each swatch in the 색상·투명도 section (ADR 0034) is a button. Clicking it opens one
  popup owned by the settings window, next to the swatch. Opening another swatch first keeps
  the color of the one already open.
- **Contents.**
  - A saturation/brightness square and a hue bar. Both work with the pointer and with the arrow
    keys: 2% steps on the square and 3° steps on the bar.
  - The `#RRGGBB` box, the current hue, saturation and brightness, and a before/after sample.
  - 24 recommended colors.
  - Up to eight recently chosen colors, newest first. They are kept for this run only and are
    never stored.
  - Contrast checks.
  - 취소 and 선택.
- **Draft semantics.** Every change writes the Draft color text, so the widget previews at
  once.
  - **선택**, or a click outside the popup, keeps the color.
  - **취소** or Esc restores exactly the text the box held when the popup opened, even if that
    text was invalid.
  - Neither of these saves. The settings window's 적용 and 확인 save, and its 취소 discards, as
    before (ADR 0003, ADR 0027).
  - Esc inside the popup does not trigger the settings window's 취소.
- **Contrast.** The contrast check informs and never blocks.
  - **Pairs checked:** header text against the header and today header backgrounds; cell text
    against the cell and current-period backgrounds; each of those backgrounds against its text.
  - **Ratings:** at least 4.5:1 reads as 잘 읽힙니다; at least 3:1 as a caution; below that,
    읽기 어렵습니다 · 저장은 할 수 있습니다.
  - **Opacity note:** a background below 100% opacity adds a note that real contrast depends on
    the desktop.
  - **Not checked:** the clock text, which sits directly on the desktop, and the border. For
    these the window says why it shows no check.
- **No alpha.** The window offers no transparency input, so R11 holds. Opacity stays on the
  ADR 0026 sliders. A screen eyedropper is deferred.
- No setting, storage format or Apply rule changes.

## Consequences

Colors no longer require typing hex codes, and the hex box remains for exact values. Recent
colors reset on restart, so profiles, presets and backups are unchanged.

## Verification

- Automated tests cover:
  - HSV round trips, known hues and gray hue retention.
  - WCAG reference ratios.
  - Draft writes and preview.
  - Cancel restoring the opening text, including invalid text.
  - Confirm recording a recent color, and the eight-color limit.
  - Invalid hex input.
  - Contrast levels, opacity notes and non-blocking Apply.
  - Panel pointer, keys, chips and Esc on unshown WPF objects.
  - One adjuster at a time in the settings window.
- The popup itself is not opened in tests.
- Still to check natively:
  - Popup placement near screen edges.
  - Dragging feel.
  - Focus and Esc.
  - Rendering at 100/150/200% DPI.
