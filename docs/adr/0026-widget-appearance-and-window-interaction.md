# ADR 0026 — Widget appearance and window interaction

Status: **Accepted — explicit implementation approval 2026-10-08; native verification pending**

## Decision and scope

Implement the approved four features in two stages: table appearance first, window interaction
second. The user approved this scope on 2026-10-08, tied to the 31-file plan. Starting
revision is `866dd6ba0fe558ab86d5fdc17fed0fea25c5615c` on `codex/data-interchange`.
[Product Contract](../PRODUCT-CONTRACT.md#widget-appearance-and-window-interaction--approved-2026-10-08)
is the behavioral authority. This supersedes the numeric-settings exclusion of ADR 0019 and
appearance exclusions of ADRs 0014/0016 only within this scope. Data interchange remains unchanged.

## Appearance and durable ownership

`DisplayConfiguration.Table` is immutable Desktop data. Four doubles preserve exact opacity
without percentage/byte truncation. Brushes receive opacity; text does not. Header and cell
use existing typography drafts, font resolution and download restrictions. Preserve the four
clock elements and expose two separate table editors. Live preview does not commit profile data;
Apply, preset library management, Cancel/X and restore share existing transaction boundaries.

Retain the existing outer profile v5 / preset v1 / backup v1 contracts and add an optional,
strictly versioned table object to current display payloads. Older data loads defaults without
rewrites. A present table requires all fields and version 1; unknown properties/versions, null,
nonfinite/out-of-range values and unsupported font identities reject the complete candidate.
Strict older apps reject the extension. No reader drops or rewrites unknown settings.
The profile writer flushes an exact-byte safety copy before first extending a loaded old profile.
Existing explicit restore already preserves its prior revision through the recovery mechanism.
Window-state remains local, adds optional moveLocked, and refuses save after invalid/unknown
load or an external byte change. Before first extending valid legacy state, flush its original
bytes too. Backups are immutable evidence, never an automatic rollback target.

## Windows boundary

Only MainWindow uses WindowStyle.None, AllowsTransparency and transparent background.
The placement adapter corrects HWND z-order on window-position/activation/enable events with
SWP_NOACTIVATE and HWND_BOTTOM. There is no periodic activation/z-order loop. Correction is
suspended for visible owned WPF dialogs or a disabled owner of native dialogs; normal dialog
chrome and foreground behavior are retained. Explicit tray/second-instance activation remains
an explicit action, followed by bottom placement. No worker changes the foreground on ticks.

Body drag starts only after the system threshold. Preview mouse-down leaves focus/editing
routing intact; second clicks do not arm movement. Inputs/buttons/scroll controls and resize
edges cannot arm dragging. DragMove reuses the existing native gesture completion path.
MoveLocked gates both body dragging and SC_MOVE; resize stays enabled. Native hit tests use
six-DIP edge regions. A one-alpha resize rim and table hit surface provide layered-HWND
coverage for resize and empty cells even when their configurable brushes have zero opacity.
These hit surfaces are separate from the exact stored brush opacity values and opaque text.
The interactive table has a composited alpha floor of 1/255: a brush opacity p over the hit
surface renders p + (1-p)/255, rather than claiming true zero-alpha table pixels. This follows
[Windows layered-window hit testing](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows).
Unused main
areas remain fully transparent and can be passed through by Windows. Native validation is
still required; no claim of complete transparent-area pointer interception.

Numeric size/lock uses a normal owned dialog. It has no preview side effects, persists before
publishing preferences, retains input on failure, and discards unapplied input on Cancel/X.
Preferred DIP intent is still distinct from measured content size, target HWND DPI, physical
placement and work-area fitting. Geometry reset preserves movement lock.

## Verification and restoration

All builds/tests run from temporary source copies. Original approved files were copied before
editing; hashes and restoration instructions are retained separately from the repository.
Operational user data is not accessed, modified or restored. No push, PR, main merge or deploy.
Local Linux Core-only baseline: 299 passed, zero failed/skipped. New WPF/storage tests are
compiled for Windows but cannot run on Linux. Their pre-implementation checks exposed missing
APIs at compilation; these are not behavioral RED/GREEN execution evidence.

Pending native Windows: actual transparency, zero-alpha region input, body drag versus cell
edit/F2, locked resize, tray return, settings/edit/file dialog visibility, and 100/150/200% DPI.
A Linux cross-build cannot certify OS behavior. Final automated/source review evidence will
be appended after verification. No new testing framework or repository reorganization.


### Final local evidence — 2026-10-08

- 30 unique changed files: 27 existing and three new; all within the approved 31-file allowlist.
  Existing ProfileBackupFile production code did not need modification: ProfileJson carries
  the table extension through its existing backup import/export boundary.
- Linux Core-only regression: 299 passed, zero failed/skipped. This suite verifies unchanged
  timetable/time/data-interchange behavior and does not include the new Desktop tests.
- Full Windows-targeted solution cross-build on Linux: zero warnings/errors, including new
  Desktop tests. Full test execution was attempted and aborted before tests because
  Microsoft.WindowsDesktop.App is unavailable on this Linux host. No WPF pass count is claimed.
- Independent read-only review identified a missing pre-format backup when only a saved preset
  lacked table settings. The writer now checks every preset and the active display, backed by
  PresetOnlyAppearanceUpgradeStillBacksUpOriginalProfile. Legacy v4 JSON-shape assertions now
  verify unchanged old fields separately from injected table defaults.
- A further independent assessment confirmed the zero-alpha empty-cell input risk; table input
  now has the documented one-alpha backing surface. Actual HWND input/rendering remains pending.
- Original 28 file copies match recorded SHA-256. Scope checks, local documentation targets,
  and git diff --check passed. Original checkout/branch and 866dd6b baseline remain preserved.
  No source material, user operational data, remote branch, PR, main or deployment was changed.

Evidence logs, exact changed-file inventory, source hashes, patch and restoration guide are
kept outside the repository to respect file scope.
The review found no remaining actionable source defect in the inspected scope. This does not
replace the pending Windows test run and native acceptance list above.

### Subsequent Windows evidence — 2026-10-19

The dated pending statements above describe the 2026-10-08 checkpoint. At source
`5f90852e263b61ec44813ae925769b9e192e4398`, Windows Debug build and the full
1,294-test suite passed on a laptop; 18 isolated bootstrap cases passed too.
[The native verification report](../WINDOWS-NATIVE-VERIFICATION-2026-10-19.md)
records scoped 100% DPI checks and the precise evidence boundaries.

With target foreground and pointer ownership verified, left-edge resizing worked with
movement lock off/on and transparent-cell double-click opened the editor. Initial
no-change pointer observations are not confirmed resize/input defects. Actual screen
captures at 21:26–21:30 KST showed desktop icons through transparent cells, with poor
contrast where black text overlapped dark background/icons. Window-only captures had
not established that composition. Actual Korean IME composition and 150/200% DPI remain
unverified, alongside the report's other untested combinations. No behavioral decision,
source code, deployment or complete native acceptance is introduced by this evidence note.
