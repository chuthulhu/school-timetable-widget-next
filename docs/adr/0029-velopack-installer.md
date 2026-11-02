# ADR 0029 — Velopack per-user installer

Status: **Accepted — user-approved 2026-11-02; installer build automated, install/uninstall native verification pending**

Date: 2026-11-02

## Context

A1 approved a per-user install with minimal administrator dependence, an autostart option and
a normal uninstall. The installer technology, runtime delivery and uninstall data policy were
DEFERRED. On 2026-11-02 the user chose four things:

- Velopack.
- A self-contained .NET runtime.
- Always keeping user data on uninstall.
- CI builds plus a GitHub Release for each version tag.

## Decision

- **Package.** Velopack `1.2.161` (NuGet `Velopack`, CLI `vpk`) packs a `dotnet publish -c Release
  -r win-x64 --self-contained` output. The pack ID is `SchoolTimetableWidgetNext`, and the main
  executable stays `SchoolTimetableWidget.Desktop.exe`.
- **Install location.** The setup program installs per user, without elevation, to
  `%LOCALAPPDATA%\SchoolTimetableWidgetNext\current\`. That path stays the same across Velopack
  updates, so an autostart registration made by an installed app remains valid. Velopack's
  default Start-menu and desktop shortcuts are kept.
- **User data is kept.** The profile, backups, recovery evidence and the downloaded font cache
  stay in `%LOCALAPPDATA%\SchoolTimetableWidget`. Velopack deletes its whole install folder on
  uninstall, so the pack ID must never equal that profile folder name. The workflow states this
  next to `PACK_ID`. Uninstall never deletes, moves or rewrites user data. Reinstalling continues
  with the same profile.
- **Entry point and hooks.** `Program.Main` (the `StartupObject`) calls `VelopackApp.Build().Run()`
  before WPF, single-instance election or any profile access. Velopack hook invocations
  (`--veloapp-*`) therefore run and exit without starting a second widget. Outside an install,
  `Run()` returns and the app starts normally.
- **Uninstall cleanup.** The before-uninstall hook removes the per-user Run value
  `SchoolTimetableWidget` only when it still equals the installed executable's own command
  (ordinal, case-insensitive). A registration pointing elsewhere is left alone, such as a
  development build or a command with extra arguments. Failures are swallowed, so cleanup never
  blocks uninstall. This fulfils ADR 0021's "future uninstall should remove the owned value".
  Autostart stays default OFF after install.
- **Builds.** `.github/workflows/installer.yml` builds the installer on every pull request and
  main push as a workflow artifact, versioned `0.1.0-ci.<run>`. A `v<SemVer>` tag builds that
  version and creates a GitHub Release with the Velopack output: the setup executable, portable
  zip, full package and release feed.
- **No signing.** Without a code-signing certificate, Windows SmartScreen warns on first run
  and the user must choose "More info → Run anyway". Signing remains a separate decision.

## Not decided here

- **Updater.** No update check, download or apply is implemented. Velopack's auto-apply on
  startup has nothing to apply without an update manager. Update source, channel, prompts and
  rollback remain DEFERRED.
- **Other gaps.** Supported Windows versions, an MSI/enterprise deployment and signing remain
  DEFERRED.

## Verification

- Automated:
  - Unit tests of the owned-value cleanup against an in-memory store.
  - The CI installer job publishing, packing and uploading the installer.
- Not verified:
  - Running the setup program on a real PC.
  - First-run SmartScreen behaviour.
  - Shortcut creation.
  - Uninstall while the widget is running in the tray.
  - That uninstall leaves `%LOCALAPPDATA%\SchoolTimetableWidget` intact on a real machine.

  These are required native checks before the first tagged release.
