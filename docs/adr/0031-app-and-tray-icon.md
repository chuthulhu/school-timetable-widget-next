# ADR 0031 — Application and tray icon

Status: **Accepted — user chose concept A on 2026-11-03; native verification pending**

Date: 2026-11-03

## Context

The tray used Windows' generic application icon and the executable had none. The user asked
for icon designs. They were shown four concepts at 256, 48, 32 and 16 px on light and dark
taskbars, and chose concept A.

## Decision

- **Design.** A rounded teal square uses the guideline palette (ADR 0028): `#3A7D7B`→`#255655`,
  and `#2F6766` at small sizes. It holds paper-white tiles with one amber current-period tile,
  `#F2B84B`, echoing the ADR 0030 tile layout. Sizes of 32 px and larger show a 3×3 grid. Sizes
  of 16–24 px use a 2×2 variant, so the tiles stay distinct on light and dark taskbars.
- **Sources and outputs.** `Assets/Icons/app.svg` and `tray.svg` are the sources. `app.ico`
  holds frames at 16, 20 and 24 px (2×2) and at 32, 40, 48, 64, 96, 128 and 256 px (3×3). It is
  the project's `ApplicationIcon`, so the exe, the Velopack shortcuts and the setup program
  share it; the workflow passes `vpk --icon`. `tray.ico` holds the 2×2 variant at 16–64 px. It
  is an embedded resource, and the tray loads the frame for the current small-icon size, not
  a resampled one.
- **Regeneration.** The `.ico` files are committed. `scripts/build-icons.mjs` regenerates them
  from the SVGs. It is optional and needs Node with Playwright/Chromium and ImageMagick.

## Verification

- Automated: the tray uses the embedded image at the small-icon size and not the generic
  icon; the tray resource carries every DPI frame; the built executable's associated icon
  shows the amber centre tile.
- Not verified: native appearance in the tray, Explorer, the Start menu and the setup
  program at 100/150/200% DPI.
