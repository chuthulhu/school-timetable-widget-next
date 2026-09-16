# Window placement and preferred size

Status: **AUTOMATED VERIFIED — USER NATIVE UX APPROVED**.
Authority: approved milestone request and [ADR 0019](adr/0019-machine-local-window-placement.md).
Start: main, HEAD/origin/main acf911c68a70ce44f9905a8c50c2398e38b98980, clean.

## File and ownership

Production location: %LOCALAPPDATA%\SchoolTimetableWidget\window-state.json.
DEBUG --dev-profile-directory and preview modes isolate both profile and window state in TEMP.
windowStateVersion is 1. preferredWidth/preferredHeight and left/top are DIPs. left/top are
relative to the saved monitor work-area origin; monitorHint is a best-effort Windows device
name, not permanent identity. Current work-area origin, size and HWND DPI are authoritative.
The default preference is 800 x 600 at a safe 40-DIP inset, clamped as necessary.

Window state is machine-local convenience. Profile data is portable user/application data.
.stwbackup contains profile data; it excludes window state. Backup restore, degraded state,
Recovery Required, Settings Preview/Apply/Cancel and font availability do not own or overwrite
window intent. Derived applied geometry, content minimum and scroll position are never saved.

## Gestures, layout and persistence

A feature session owns preferred bounds and dirty intent. The Windows controller observes
WM_ENTERSIZEMOVE, WM_MOVING, WM_SIZING and WM_EXITSIZEMOVE, reads the actual normal rectangle
at completion and saves once. Resize edges limit which dimensions/position axes are captured.
Move alone preserves preferred size even when content has enlarged the visible window.
Canceled/no-change gestures and minimized/maximized samples do not create writes.
WPF location/size notifications schedule layout only; no timing heuristic classifies users.

WindowContentMinimum retains measured fixed headers/non-client space and the timetable body
minimum. The placement calculator combines that minimum with preferred bounds and current
work area. Cap overflow and retain body-only scrolling. Short content restores preferred
size/position. Suspend fitting during a user gesture to avoid fighting the drag, then measure
again on completion. Normal clock ticks neither measure placement nor write window state.

Load before Show, select the saved monitor or primary fallback, fit initial bounds before the
first visible layout, then reconcile the measured content minimum. On display/work-area/DPI
messages use the existing coalesced WPF layout path. Use physical virtual-desktop coordinates
only at the Windows boundary and GetDpiForWindow for current logical/physical conversion.
The application manifest declares PerMonitorV2. Negative monitor origins remain valid.

The reset menu is **창 위치/크기 초기화**. It resets only preferred window bounds and uses the
current available monitor. It neither resets nor saves timetable, display, presets or fonts.
No numeric settings controls were added.

The small file is limited to 4 KiB on read. Reject malformed, duplicate/unknown/missing fields,
unsupported versions, non-finite/invalid/extreme dimensions or coordinates. Ignore invalid
state without touching profile recovery status. Save uses complete serialization followed by
same-directory temp write, Flush(true), close and rename; failure leaves previous bytes intact.
Only dirty preferred state is retried on normal close. No applied geometry capture at shutdown.

## Verification — 2026-09-16

Final gate: dotnet restore, dotnet build --no-restore, and dotnet test --no-build --logger
"console;verbosity=normal" succeeded. **953/953 passed**, failed/skipped 0; 46 new cases retain
the existing 907 cases. Build warnings/errors **0**. git diff --check passed.
Log: %TEMP%/stw-window-placement-approved-final-tests.log.

The first full run was 950/951: the existing exact context-menu expectation still listed nine
commands after the reset entry was added. The complete ordered expectation now includes ten
commands; existing import/copy/editor-routing assertions remain. This was a menu-fixture update,
not a resource-concurrency failure or a weakened/removed test. Two later menu-click cases
increased the final total to 953.

The unshown actual-HWND initialization test passed alone (1/1); the two actual MenuItem.OnClick
object/event paths passed together (2/2); the placement WPF class and full suite passed.
WPF tests retain the shared resource-lifetime gate. No sleeps, native keyboard/pointer/clipboard
actions or foreground test dialogs are used. The HWND test uses the test host's DPI context,
not the production manifest. Fake monitor/DIP tests cover 96/120/144 DPI, negative origins,
saved/missing monitors, smaller/larger work areas and taskbar reservations.

Coverage includes restart, completed gestures, auto size/position separation and content shrink,
width-only resize after auto growth, no-change/non-normal gesture exclusion, 100 actual clock
refreshes without placement work/writes, malformed/extreme state, failed rename/retry, reset,
profile/backup isolation, and degraded/Recovery Required independence.

## Native evidence and user approval

Codex launched the normal built app directly outside the sandbox using the same isolated TEMP
profile/window-state directory for six runs. The user performed actual gestures, inspected the
screen, and closed each run with X. Each owned PID disappeared; all stdout/stderr logs were empty.
No forced termination, synthetic native input or production data modification was used.

| Run | Evidence |
| --- | --- |
| 1, PID 35228 | User confirmed visibility, normal move/resize and close. Saved preference: 846 x 724 DIP, (40,40), DISPLAY1. |
| 2, PID 37352 | User confirmed restart restoration. Read-only host inspection: physical (40,40)-(886,764), 96 DPI, PerMonitorV2, DISPLAY1 work area (0,0)-(1920,1032). |
| 3, PID 46976 | After stopped TEMP content was changed to 12 lines, actual bounds became (40,0)-(886,1032). Height 724 -> 1032 and top 40 -> 0; preferred file stayed byte-identical before/during/after the run. User confirmed full content/scroll access and close. |
| 4, PID 46644 | With short TEMP content restored, actual bounds returned to (40,40)-(886,764). User confirmed the return, then moved to DISPLAY2 and closed. Saved size stayed 846 x 724; new relative position (1020,151), hint DISPLAY2. |
| 5, PID 49456 | Same-monitor restart produced physical (2940,151)-(3786,875), 96 DPI / PerMonitorV2, within DISPLAY2 work area (1920,0)-(3840,1032). User approved UX. |
| 6, PID 53140 | User explicitly performed the reset and confirmed it. After close, disk held default 800 x 600, relative (40,40), DISPLAY2. Profile content was unchanged by reset. |

The user initially approved reset without clicking its menu; the unchanged on-disk value caused
the commit gate to pause. The user clarified the omitted click, requested reopening and then
confirmed the actual reset. This resolved the evidence discrepancy without production changes.
The explicit UX approval remains accepted. The extra two menu-click tests are retained.

Both physical monitors were 1920 x 1080 / 96 DPI with 1032-pixel work-area height. Actual secondary
monitor move/restart is verified. Mixed DPI, negative-origin and missing-monitor fallback are
fake-provider/object evidence, not claimed hardware disconnect/DPI-setting experiments.
In-session content shrink is WPF object evidence; tall-to-short native restart is verified above.

A sandbox read returned HWND 0 while the same-desktop host read found the responsive normal
window. This was an inspection-boundary difference, not an app failure or visibility proof.
Actual visibility came from the user. No system clock/display settings or clipboard were changed.
TEMP directory: %TEMP%/stw-window-native-cada5a7e95044a48ba8c4ad6ef6f49de; it contains fixture,
preferred snapshots and production-file before/after evidence. Production profile/window-state
existence, SHA-256 and modification times remained unchanged.

## Final self-audit

No outstanding P1/P2 in the inspected implementation and verified scope. Only completed normal
user gestures/reset change intent. Ordinary SizeChanged/LocationChanged, automatic measurement,
DPI/work-area correction and shutdown never capture applied values. No per-pixel or per-tick
writes. A move keeps preferred size, and one-axis resize does not adopt auto size on the other.

Missing monitor uses current primary fallback; current work area and HWND DPI govern the clamp.
Negative virtual origins are valid. Width/height cap and body-only scrolling preserve the existing
horizontal design. No minimized/maximized persistence or canonical work-area/DPI snapshot.

Corrupt local state does not degrade profiles. Local movement cannot rewrite profile.json,
enter .stwbackup, be overwritten by restore, clear Recovery Required or roll back on Settings
Cancel. No numeric Settings UI, new clock read, package or display-topology framework. The
reset affects only local preferred bounds. Tests/docs/ADR match the approved ownership change.

Native message references: [enter](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-entersizemove),
[exit](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-exitsizemove),
[resize edges](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-sizing).
