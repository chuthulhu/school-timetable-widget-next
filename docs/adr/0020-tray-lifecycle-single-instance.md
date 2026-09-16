# ADR 0020 — Close-to-tray, explicit exit and session single instance

Status: **Accepted; implemented, automated verified and user native UX approved**. Date: 2026-09-16.
Authority: explicit Tray Show / Hide + Exit Lifecycle + Single Instance milestone request.

Normal MainWindow X/Alt+F4 cancels Closing and hides the existing window. Application uses
OnExplicitShutdown. Tray Exit permits closing and shuts down; Windows SessionEnding also
permits shutdown without confirmation or a modal-editor veto. Hidden runtime remains active.

Use the inbox System.Windows.Forms.NotifyIcon on the WPF dispatcher thread with its existing
Windows message pump. Use a cloned Windows Application icon until branding is separately
approved. ShowInTaskbar is false; no Alt+Tab, always-on-top or desktop embedding hacks.
Menu: current visibility toggle, separator, Exit. Left double-click toggles visibility.
Show reuses the window/runtime/viewed week, refreshes current status, fits current monitors
without changing preferred bounds, restores Normal when needed and activates best effort.

An open owned editor/dialog takes precedence over show/hide. Bring the deepest dialog forward;
Exit requests that it be closed first, preserving its Draft. A disabled HWND/native popup also
blocks these actions. No generic dirty-state framework is introduced.

First normal close-to-tray requests an informational balloon. A request counts even if Windows
suppresses its display. Persist only this receipt in versioned machine-local tray-state.json,
separate from profile, backup, presets and window-state. Failure does not degrade profile or
repeat the notice in the same run; invalid/missing receipt permits a fresh request.

Acquire a named mutex before profile/runtime/window/tray startup, scoped to current user AND
current session using .NET NamedWaitHandleOptions. Secondary sets a named AutoReset event and
exits without loading profile. Create/open event before acquiring ownership so an early signal
survives primary startup. Primary listener queues the show request on the WPF Dispatcher.
Stop listener before disposing UI/runtime/profile; release mutex last. Abandoned ownership
can be acquired by a new primary. Do not swallow unexpected lifecycle defects.

Production uses one stable scope. Existing development previews use a separate stable scope;
DEBUG explicit TEMP directories use a normalized directory-derived scope for isolated tests.
This is a development isolation rule, not production multi-profile support.

No autostart, class notification, update, installer, new IPC framework or third-party package.
Earlier deferred tray/normal-X-exit descriptions are superseded for this implemented scope.
[Lifecycle and verification](../TRAY-LIFECYCLE.md).
