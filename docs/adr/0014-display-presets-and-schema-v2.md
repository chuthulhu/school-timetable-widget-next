# ADR 0014 — Display presets and profile schema v2

Status: **Accepted**. Date: 2026-09-11.
Authority: explicit Styling / Display Presets Foundation milestone request.
Accepted records the requested behavior and storage decision. Native UX acceptance was
subsequently confirmed on 2026-09-11; see the Display Settings verification record.

## Decision

Display configuration belongs to Desktop Features/DisplaySettings. Immutable input records
separate preset identity, layout, per-element typography and format/visibility choices.
Standard, Digital, Compact and Minimal provide initial configurations. Selecting a preset
replaces Draft with its defaults; subsequent independent edits never reapply the preset.
Reset restores the currently selected preset's defaults as an unsaved preview.

Time, Date, Weekday and Status each own a logical FontSelection, size, weight and style.
FontSelection has an explicit source kind; only System is supported in this schema.
The Windows adapter enumerates Fonts.SystemFontFamilies and resolves family names locally.
Missing names remain saved as requested while rendering falls back to Segoe UI or the
system message font. No absolute paths, remote references, downloaded/cache/font binaries
or provider framework. Additional sources need explicit schema/resolver support later.

P2 is unchanged: Open copies committed to Draft/baseline; valid Preview changes only owned
display runtime; Apply validates and saves before updating committed/baseline and stays open;
OK closes only on successful Apply; Cancel/X restores controls and preview to the last
successful baseline. Invalid input retains the last valid preview. A save failure retains
Draft, preview and old committed/baseline with an error. Exactly one session owns preview.

The existing formatter derives independent date/time/status plus weekday, Korean AM/PM
and 12-hour time from its already captured ApplicationTimeSnapshot. Display preview
reformats those cached facts without a clock read. No new timer or Core presentation input.
The same WPF elements remain across configurations and ticks. Layout/font measurement
runs on explicit display changes only; worst-case slots reserve ordinary tick geometry.

## Versioned storage

Schema 2 adds required profile.display to the strict storage DTO; all former fields and
validation remain. Explicit separate v1 DTOs read exactly the old shape and inject Standard
defaults into the runtime snapshot. Load never rewrites v1. The next successful user save
of any feature writes the full schema 2 snapshot. Corrupt v1/v2 and unknown versions still
fail closed. A missing installed family is a valid value, not corrupt profile data.

All feature callbacks preserve Current.Display; display saves use the latest other feature
inputs. Uncommitted display preview cannot leak into a timetable/lunch/date save. Existing
same-directory temporary write, Flush(true), rename and publish ordering are unchanged.
There is no startup/Cancel/exit save, downgrade writer, auto repair or partial commit.
Old schema-1-only binaries may reject schema 2 as unsupported; downgrade is not supported.

## Superseded and deferred scope

This implements the display portion previously deferred in ADR 0008, and extends ADR 0012's
schema. A4/A8's always-visible status and fixed 24-hour format are Standard defaults:
explicit user display choices now allow hiding date/weekday/status and using 12-hour time.
Countdown meaning, 35 cells, week navigation and compact column dates are unchanged.

P3's content minimum is remeasured on display changes and rollback. A horizontally scrollable
header handles excessively wide typography without forcing an enormous window width.
Full persisted preferred geometry, whole-app responsive layout and all-DPI behavior remain
future work. Size numbers use WPF device-independent units, not a promised point conversion.

Title and independently styled AM/PM can extend the same element model; AM/PM is already a
separate text element using Time typography today. Bundled/online fonts, local font import,
named custom presets, colors/themes and final Fluent redesign remain deferred.

Implementation and verification: [Display Settings](../DISPLAY-SETTINGS.md).
WPF font API reference: [Enumerate system fonts](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-enumerate-system-fonts).
