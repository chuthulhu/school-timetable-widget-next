# Tray lifecycle and single instance

Approved contract: [ADR 0020](adr/0020-tray-lifecycle-single-instance.md).
Implemented; **993/993 automated tests passed; user native UX approved**.

## User behavior

- X / Alt+F4 hides the existing MainWindow and leaves the process/tray alive.
- Tray menu shows 위젯 숨기기 when visible and 위젯 보이기 when hidden, then Windows 시작 시 실행, a separator and 종료 (ADR 0021).
- Left double-click toggles; single click has no application action.
- 종료 terminates the application after editors are closed. With an open editor it brings that
  dialog forward and requests “열린 편집 창을 먼저 닫아 주세요.” without discarding Draft.
- Show reuses runtime and viewed week, refreshes/re-measures, restores Normal if necessary,
  fits current work areas preserving preferred placement, and activates best effort.
- ShowInTaskbar is false. The fallback Windows Application icon has tooltip School Timetable Widget.
- Windows logoff/shutdown bypasses the close-to-tray/editor guard without confirmation.
- Hidden does not suspend the application clock or the existing one-second refresh.

## Ownership and failure boundaries

WidgetTrayLifecycle owns visibility/close/exit policy through small window/tray interfaces.
WpfWidgetWindow owns WPF/native modal detection and show integration. WindowsTrayIcon owns
NotifyIcon, context menu and cloned icon. App owns startup ordering, refresh/profile resources,
SessionEnding and final shutdown. Placement retains its existing user-gesture persistence policy.

WindowsSingleInstance owns a user/session-scoped named mutex and AutoReset activation event.
SingleInstanceStartup gates all profile/UI initialization. Secondary signals only, then exits.
Event exists before ownership election; dispatcher marshals activation. Stop/drain background
wait callbacks before resource disposal; pending dispatcher actions are guarded. Release ownership
after profile disposal. No profile reload/recovery approval occurs on activation.

Machine-local tray-state.json v1 contains exactly trayStateVersion: 1 and closeNoticeRequested: true.
Request receipt is written atomically only on the first close-to-tray request. No startup rewrite,
no profile schema change, no backup/preset inclusion. Invalid/unsupported/oversized receipt is
ignored; read/write failure affects only this optional receipt. In-run suppression remains.
OS balloon delivery and foreground activation remain best effort.

Owned WPF dialogs are followed to the deepest visible child. Disabled owner HWND/native popup
detection also protects file dialogs/message boxes. Actual shell rendering, input and modal OS
behavior require native smoke; unshown object tests do not establish those results.

## Verification

Baseline: 953 tests at 77bf2e14437994740379627c899f060d2020cd4c.
40 new tests cover normal close/Exit/session end, editor guards with real Draft preservation,
real Forms tray objects without shell publication, receipt corruption/atomic failure/restart,
actual named mutex/event contention and dispatcher activation, profile/recovery byte preservation,
viewed-week/current-clock behavior and preferred bounds after changed monitor constraints.
An actual DEBUG secondary executable exited 0, sent its signal and created no profile files.
Restore succeeded after refreshing stale NuGet metadata outside the sandbox; build 0 warnings /
0 errors; full suite 993/993; diff check clean. WFO0003 suppression is documented in the project:
this WPF application retains manifest DPI ownership and does not use WinForms startup.
Self-audit found no unresolved P1/P2: close/Exit/system paths separate, no new window/profile on
activation, independent local notice state, modal Draft retained, no scope expansion or catch-all
error suppression. Native evidence and its limits are recorded below.
Production user data and system settings are not used by isolated TEMP verification.
Historical window-placement X/process-exit evidence predates this approved lifecycle change.

## Platform references

[NotifyIcon](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-10.0),
[ShutdownMode](https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.shutdownmode?view=windowsdesktop-10.0),
[SessionEnding](https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.sessionending?view=windowsdesktop-10.0),
[NamedWaitHandleOptions](https://learn.microsoft.com/en-us/dotnet/api/system.threading.namedwaithandleoptions?view=net-10.0),
[wait unregister completion](https://learn.microsoft.com/en-us/dotnet/api/system.threading.registeredwaithandle.unregister?view=net-10.0).

## Native checkpoint — notice investigation, 2026-09-16

Isolated TEMP primary PID 64440 remained responsive after the user's close test. User reported
the notice was not visible. The TEMP tray-state.json receipt was written at 15:04:51 KST,
after the ShowBalloonTip request path; this establishes request-path completion, not shell display.
Read-only inspection in the actual user session found
HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications\ToastEnabled = 0.
SHQueryUserNotificationState returned S_OK / QUNS_ACCEPTS_NOTIFICATIONS (5), which describes
the current interruption state and does not negate the separate disabled notification setting.
Disabled Windows notifications are the likely explanation; actual balloon display is NOT verified.
No Windows setting, receipt or live process was changed to force a replay. The approved
request-once policy remains in place. The user subsequently approved the UX with this limitation.

## Native acceptance — 2026-09-16

The user confirmed tray menu Show, double-click hide/show, and preservation of position, size
and viewed week. They then hid the widget for secondary-launch testing and used tray Exit.
Visible-state secondary PID 66528 and hidden-state secondary PID 65632 both exited with code 0;
primary PID 64440 remained alive. After the user's tray Exit, PID 64440 was absent. Restart
with the same TEMP directory created primary PID 59704 successfully; final user response: 승인.
The original profile.json, window-state.json and tray-state.json existence/hash/mtime checks
were unchanged. No production profile or OS notification setting was modified.

Native UX approval covers the exercised tray/show/close/activation/Exit/restart flow, including
the final prompt about icon/taskbar/cleanup appearance. Actual balloon rendering remains
unverified because Windows notifications are disabled. The approval is not evidence of a
separate native Alt+F4 test, all modal/native file-dialog variants, monitor hotplug, or real OS
logoff/shutdown. Those paths have the documented object/event tests and source integration;
no real system shutdown was performed.

Final restart cleanup: user selected tray Exit for PID 59704; process absence was verified.
An earlier final build attempt was blocked by that live executable's file lock (MSB3026/3027/3021).
After normal tray Exit, build completed with 0 warnings and 0 errors. Original state
existence/hash/mtime checks remained unchanged. No diagnostic process was forcibly terminated.

## Autostart integration — 2026-09-17

Opening the menu refreshes OS registration; only clicking Windows 시작 시 실행 changes it.
X/show/hide/notice/Exit/session end/secondary activation do not change registration. Every new
primary shows MainWindow; prior hidden state is never persisted. Autostart native evidence is
tracked separately in [AUTOSTART](AUTOSTART.md), not inferred from earlier tray approval.
