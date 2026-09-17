# Windows autostart

Status: implemented; **1,032/1,032 automated tests passed; user native UX approved**.
Baseline: main / origin/main b5076ed8e89d147eee629cddeada722e48735d8b, clean, 993 tests.
[Approved ADR 0021](adr/0021-per-user-windows-autostart.md).

## Registration and UI

Tray **Windows 시작 시 실행** explicitly manages REG_SZ `SchoolTimetableWidget` in
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Missing means OFF. Opening the menu
reads current OS status; only explicit clicks write/remove. No startup mutation or polling.
Stale entries are unchecked with a repair tooltip; selecting registers the current path.
Unavailable reads show a disabled indeterminate item; reopening retries.

The command is exactly the quoted current `SchoolTimetableWidget.Desktop.exe` ProcessPath.
Spaces/Unicode are preserved. No DLL, shell, extra argument or elevation. Unsupported hosts
and commands longer than Windows Run's 260-character limit are rejected without registration.
Write/delete read-back establishes success. Expected access failures display short Korean errors
without exception details. UI reflects fresh actual status; failed observation is unknown.
Do not claim rollback after a read-back failure or blindly overwrite concurrent external changes.

Features/Autostart owns status/verified mutation through IAutoStartRegistrationStore.
Infrastructure/Windows owns the exact HKCU adapter, command and tray controls. App constructs
the service inside primary startup only. No generic registry framework or profile dependency.

## Startup and data boundaries

Autostart uses normal startup: single-instance election, profile/runtime/placement, tray and
visible MainWindow. Existing taskbar-hidden policy, current monitor/DPI/work-area fallback and
degraded/recovery notices remain. No sleeps or alternative geometry. X hides for this session;
tray Exit exits without unregistering. Previous hidden state never persists to the next launch.
Second launch only activates the existing primary. The first-close notice is independent.

Profile, .stwbackup, .stwpreset, window-state.json and tray-state.json exclude autostart.
Restore does not touch registration. Degraded/Recovery Required still permit the independent
toggle without saving profile, removing markers or altering recovery snapshots.
Legacy true alone never authorizes OS mutation. Windows Startup Apps, organization policy and
security software may block execution; StartupApproved is untouched. Future uninstall should
remove the owned value; installer/updater, delay, start-hidden, services and scheduler are excluded.

## Verification and native checkpoint

Tests use fake registration stores and unpublished Forms objects. Object/event and source
evidence do not establish shell rendering, native input or real login behavior.
Native plan: snapshot exact registry existence/type/data, isolate app files in TEMP, confirm
OFF/ON and exact command, Exit retaining registration, execute that exact command, check visible
window/placement/tray, second launch, X hide, OFF removal and normal Exit. Restore and compare
the original registry state and original app-file hashes/mtime. No logoff/login is requested.
Native UX approval is required before commit/push.

DEBUG supports process-local `STW_DEV_PROFILE_DIRECTORY` beneath TEMP with the same validation
as `--dev-profile-directory` (argument takes precedence). Release ignores both selectors.
This permits the exact registered command with no appended arguments and isolated app files.
Never set a user/system environment variable; document this changed storage test condition.

### Automated verification and self-audit — 2026-09-17

dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger
"console;verbosity=normal" succeeded. Build warnings/errors 0; 993 baseline + 39 new = 1,032
passed, none failed/skipped. git diff --check passed. Final log: %TEMP%/stw-autostart-final-tests.log.
New coverage: default/idempotent registration, exact quoted space/Unicode EXE, unsupported
hosts/command text/length, stale repair, expected access failures, read-back failure/mismatch,
real unpublished tray events/checkmarks/refresh, lifecycle/secondary independence, normal/
degraded/recovery byte preservation, portable backup and restore independence, DEBUG TEMP
selection. Source boundary checks restrict the production adapter to the exact owned value and
verify unchanged primary-only visible startup. They do not execute the production registry API.
Existing real mutex/event contention and secondary executable tests also pass.

Self-audit: no automatic/default-ON mutation; HKCU exact value only; no HKLM/admin/scheduler/
service/StartupApproved; no portable/local JSON autostart or hidden state; no restore/X/Exit/
secondary mutation; quoted actual apphost with no arguments; explicit stale repair; verified
UI status; event-driven refresh, no polling/sleep; no installer/updater expansion. No unresolved
P1/P2 found. Actual registry evidence is recorded separately below, not inferred from automated results.

### Native preparation — 2026-09-17

Actual-user read-only preparation found SchoolTimetableWidget Run value absent. Writable
Run-key access was opened without mutation and exact snapshot round-trip verified. Snapshot:
%TEMP%/stw-autostart-native-d36ceca209074bc58f93009eee7afba7/before.clixml.
The same directory contains native.ps1 (Prepare/Launch/LaunchRegistered/Status/Restore), owned
PID ledger and isolated profile directory. Do not overwrite the original snapshot.
Direct outside-sandbox launch created PID 33656 using the normal DEBUG EXE, no arguments,
and process-local STW_DEV_PROFILE_DIRECTORY. A TEMP placement preference was seeded (800 x 600,
120/90 DIP inset); this is storage isolation, not a production profile or real-login test.
Process was responsive; subsequent user observations are recorded below.

The first diagnostic file comparison falsely differed because absent-file fields serialized
as AutomationNull objects. Comparison now checks existence first and hashes/mtime only for
existing files. Same-user recheck passed: original production file states unchanged and Run
value still absent. A sandbox Status attempt was correctly refused by the diagnostic's user
identity guard; registry evidence must use the actual-user execution boundary. Preparation did
not mutate registration or inject native input.

### Native smoke and cleanup — 2026-09-17

User confirmed visible widget and unchecked tray item, then explicitly enabled it and confirmed
the checkmark. Actual-user registry read verified REG_SZ and the exact quoted current EXE:
`"D:\Codex\school-timetable-widget-next\src\SchoolTimetableWidget.Desktop\bin\Debug\net10.0-windows\SchoolTimetableWidget.Desktop.exe"`.
After user tray Exit, PID 33656 was absent and registration remained unchanged.

The stored command was verified against that exact EXE and directly executed without extra
arguments or a shell; process-local TEMP storage selection remained. New primary PID 36432
started, and the user confirmed immediate visibility, restored placement/size and normal tray.
Manual secondary PID 39384 exited with code 0; primary remained alive. User confirmed no duplicate
window/icon and X hiding. Read-only checks found the hidden primary responsive and registration
unchanged. User then explicitly disabled autostart; the Run value became absent. Following user
tray Exit, PID 36432 was absent. No diagnostic process was forcibly terminated.

Final same-user verification: Run value absent exactly as before; original production profile,
window/tray state and recovery file existence/hash/mtime unchanged. OFF itself restored the
original absent state, so no additional registry write was needed. Only the owned Run value was
mutated by user clicks. No real Windows logout/login, external policy bypass, native failure
dialog, native Unicode/space-path execution or all-monitor/DPI test is claimed. Those command
construction cases have automated evidence. After normal Exit and final validation, the user
explicitly replied “승인”, approving the native UX, tray wording/checkbox and no separate settings
screen. The final pre-commit same-user check again found the Run value absent, original files
unchanged and all three owned PIDs absent. The user's conditional ordinary fast-forward commit/
push authorization is therefore satisfied. No force push or remote/system/credential change.

## Platform references

[Windows Run](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
documents per-user logon execution and the 260-character command limit.
[Environment.ProcessPath](https://learn.microsoft.com/en-us/dotnet/api/system.environment.processpath?view=net-10.0)
provides the actual process executable.
