# Cloud development

Codex cloud can edit this Windows desktop application's C#, XAML, tests and
documentation. Linux preparation builds Core only. The existing test project
targets Windows and references Desktop; it cannot run on Linux, even when a
change affects only Core. Product contracts and Accepted ADRs still apply.

## Linux setup

In the current Codex environment setup flow, review this command as the Install
script from the checked-out repository root:

```bash
source ./scripts/setup-codex.sh
```

The script requires Linux, Bash, curl and the .NET SDK's normal Linux system
dependencies. A compatible SDK selected by `global.json` is reused. Otherwise
the official CI installer installs the exact baseline SDK into `$HOME/.dotnet`
(or `STW_DOTNET_DIR`). No sudo, credential or system configuration is required.
Missing system prerequisites must be reported rather than silently installed.
It restores and builds only Core, failing at the first error. It does not run
the app, access production data, register autostart, or probe NTP.

Setup shell exports may not survive into a separate task shell. If installation
used the default location, begin a later command with:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
dotnet build src/SchoolTimetableWidget.Core/SchoolTimetableWidget.Core.csproj --no-restore
```

Use the same explicit directory when `STW_DOTNET_DIR` was customized. If package
caches are reset, rerun setup with network access. Never assume an installed SDK
or restored assets survived an environment reset. Do not store credentials in
the repository or in this script.

An optional Linux cross-build probe, separate from the default setup, is:

```bash
dotnet restore SchoolTimetableWidget.sln -p:EnableWindowsTargeting=true
dotnet build SchoolTimetableWidget.sln --no-restore -p:EnableWindowsTargeting=true
```

This downloads Windows targeting packs. Success is compilation evidence only;
it neither runs WPF nor establishes that Windows tests pass. Report platform
or SDK errors honestly and use the Windows gate below. No framework retargeting
or test removal is allowed to make Linux pass.

## Network requirements

Allow HTTPS to the following package/setup hosts as needed:

- `dot.net` and `builds.dotnet.microsoft.com`: official installer and SDK.
- `ci.dot.net`: official installer's fallback SDK feed, if requested.
- `api.nuget.org`: package index and flat-container downloads.
- `globalcdn.nuget.org`: package CDN when used by a NuGet endpoint.

Repository checkout access is configured separately by the cloud platform.
No application API keys are needed. Online font catalogs and KRISS NTP are not
dependencies of restore/build/test; do not enable UDP solely for CI.
If a request redirects to another host, report it before broadening access.

Codex environment creation, Install script / Start skill review, network
permissions and private publication are configured
outside this repository. Consult current [Codex cloud environments guidance](https://developers.openai.com/codex/environments/cloud-environments).
This document supplies setup commands; it does not claim those web settings
have been applied or a cloud environment has been published.

## Windows gate

`.github/workflows/windows-ci.yml` runs on pushes to main, pull requests and
manual dispatch after it is committed/pushed. It uses a Windows GitHub runner,
the SDK policy from global.json, restore, build and the full test suite, plus
the existing bootstrap control-flow checks. It has read-only repository
permission, does not persist checkout credentials and publishes no release.
It uses the existing Debug verification policy and no package lock cache.

The workflow has not run merely because the file exists. Check its actual
result before claiming Windows validation. On a Windows development machine,
the equivalent application gate remains:

```powershell
dotnet restore SchoolTimetableWidget.sln
dotnet build SchoolTimetableWidget.sln --no-restore
dotnet test SchoolTimetableWidget.sln --no-build --logger "console;verbosity=normal"
pwsh -NoProfile -File ./tests/bootstrap-dev.Tests.ps1
```

Run each only after the preceding command succeeds. Follow
[Development](DEVELOPMENT.md) for isolated TEMP profiles and native checks.
Keyboard/IME/focus, rendering, DPI/multiple monitors, tray, actual login
autostart, sleep/resume and real-network NTP still need appropriate Windows
native evidence. An unattended CI runner is not a substitute for those checks.

## Data and continuity

Keep real profiles, `.stwbackup` exports, recovery evidence, credentials and
machine-local data out of Git. New ignore rules are preventative; they cannot
remove secrets already in Git history. Synthetic `profile-v1.json` through
`profile-v4.json` fixtures and bundled fonts/licenses remain build inputs.

School and home should use separate Git clones. Pull before starting, work on
a feature branch, and commit/push only with authorization. Do not synchronize
live `.git` directories or machine-local app profiles through OneDrive.
Start every handoff at [Continuity](CONTINUITY.md), then the relevant contracts
and ADRs. App code and data schemas are unchanged by cloud preparation.

## References

- [Official .NET CI installer](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)
- [Windows targeting on Linux](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1100)
- [GitHub setup-dotnet](https://github.com/actions/setup-dotnet)
