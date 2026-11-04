# ADR 0032 — Automatic updates from GitHub Releases

Status: **Accepted — user approved automatic updates on 2026-11-04; native verification pending**

Date: 2026-11-04

## Context

ADR 0029 shipped a Velopack installer with the updater DEFERRED. On 2026-11-04 the user
approved automatic updates. They wanted installed copies to follow new releases without
reinstalling by hand. The first release, v0.1.0, ships with this ADR so that every copy
installed from it can update.

## Decision

- **Source.** Updates come from stable GitHub Releases of `chuthulhu/school-timetable-widget-next`.
  These are the releases `installer.yml` creates (ADR 0029). Prereleases and CI builds are never
  offered. The app uses Velopack's `UpdateManager` with a `GithubSource`, so no token, server or
  telemetry is involved.
- **When.** Only an installed copy checks; development and preview runs do not. The first check
  runs one minute after start, then every six hours while the widget runs. The tray menu offers
  **업데이트 확인** for an immediate check, which reports up to date, a ready version, or failure.
- **What happens.** A newer release downloads in the background. Nothing is installed while the
  widget runs. The tray announces the downloaded version once and shows **\<version\>(으)로
  업데이트 후 다시 시작**.
  - If the user chooses that item, the app takes the normal 종료 path. An open editor blocks it,
    exactly like 종료. Velopack then installs the release after the process exits and relaunches.
  - Otherwise Velopack applies the downloaded release at the next start.
- **Failures.** A failed background check is silent and retried at the next interval. A manual
  check reports the failure.
- **User data.** Updates replace only the install folder. The profile in
  `%LOCALAPPDATA%\SchoolTimetableWidget` and machine-local state are untouched, as on uninstall.
  The autostart command stays valid, because Velopack keeps the `current` path.
- **Boundary.** `Features/Updates/UpdateCoordinator` holds the timing, single-flight checks and
  state behind `IUpdateSource`. `Infrastructure/Windows/VelopackUpdateSource` is the only
  Velopack-specific code.

## Not decided here

Code signing, update channels or staged rollout, release notes in the UI, and forced or
mandatory updates remain open.

## Verification

- Automated: coordinator tests against a fake source cover:
  - Not installed.
  - Up to date.
  - A single announcement per version.
  - Applying only on request.
  - Failure, then retry.
  - Single-flight checks.
  - The policy intervals.
  - The tray update items.
- Not verified: a real update from one GitHub Release to the next. This needs two published
  releases and an installed copy.
