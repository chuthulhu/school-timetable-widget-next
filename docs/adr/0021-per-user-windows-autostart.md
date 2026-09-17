# ADR 0021 — Per-user Windows autostart registration

Status: **Accepted; automated verified and user native UX approved**. Date: 2026-09-17.
Authority: explicit Windows Autostart milestone request, including A-mode visible startup.

The unpackaged application manages only HKCU\Software\Microsoft\Windows\CurrentVersion\Run,
value SchoolTimetableWidget (REG_SZ). Default is absent/OFF. Only explicit tray menu clicks
write/remove it. Use Environment.ProcessPath's current apphost EXE, quoted, without arguments.
No HKLM, elevation, Task Scheduler, services, StartupApproved edits or startup delay.

OS registration is the source of truth; no autostart boolean belongs in profile, backup, preset
or local window/tray JSON. This supersedes P7's earlier Settings location and backup preference
wording. Legacy true is migration report evidence only. Restore never accesses registration.

Menu: visibility toggle, Windows 시작 시 실행 checkbox, separator, 종료. Refresh on opening
and explicit actions only. Disabled = absent; Enabled = current quoted command (ordinal
case-insensitive); StaleOrDifferent = another value; Unavailable = read failure.
Stale is unchecked with explicit-repair tooltip. Unavailable is disabled/indeterminate with a
reopen/retry tooltip. Never repair automatically or inspect other applications' values.

Read-back is required before mutation success. Expected access failures yield Korean errors
and refreshed state. Failed observation is unknown, never success. No blind rollback over a
concurrent external edit. Programming defects are not swallowed. Profile transactions remain
independent; toggling cannot clear recovery markers, save profiles or unblock writes.

Normal primary startup and MainWindow.Show remain unchanged, including placement, taskbar,
tray and degraded/recovery notices. Hidden state is runtime only. X/Exit do not unregister.
Secondary activation never constructs/reads the service. Registration cannot bypass external
Windows Startup Apps, organization or security policy. Future uninstall should clean up the
owned value; installer/updater implementation remains out of scope.

Tests use fake stores. Native verification requires exact existence/type/data backup and final
restoration comparison, TEMP app files, and user UX approval before commit/push.
[Contract and evidence](../AUTOSTART.md).
