# Continuity and workspace verification

Milestone: tool-independent project continuity and local workspace consolidation.
Date: 2026-09-29. Implementation baseline:
ef6c977abbfdd9725183614701bb90bc3c03a5a8 (main and origin/main matched before work).

## Inventory and disposition

Initial tracked/untracked status was clean. Ignored inventory and `git clean -ndX` identified
only the following seven directories. Resolved paths, contents, absence of tracked files
and absence of reparse points were checked before targeted deletion.

| Category | Inventory | Disposition |
| --- | --- | --- |
| A — canonical | Source, tests, docs, ADRs, SDK/project settings, scripts and font/license assets | Retained |
| B — reproducible | Core bin (6 files) / obj (31), Desktop bin (26) / obj (740), Tests bin (196) / obj (43) | Old generated outputs removed; CLI verification regenerates current outputs |
| C then B — summarized diagnostic evidence | TestResults: two Phase 0 VSTest diagnostic logs, 108,480 bytes | Confirmed early runner/exit-0 diagnostics already summarized in DEVELOPMENT; removed after retaining this result |
| D/E — local-only or unknown in repository | No additional untracked files found | Nothing deleted |
| C/D — outside repository | Isolated semester native-test directory containing snapshots, process ledger, logs, profile/recovery/local-state files and one .stwbackup | Retained; diagnostic ownership and missing historical baseline limit safe cleanup; backup may be the sole copy |

The initial bin/obj total was 1,042 files, 42,473,145 bytes. Original source font/license assets
remain tracked; copied build-output licenses are reproducible. There was no live widget process
at cleanup inspection. No broad `git clean -fdx` was used.

The immediate development-folder inventory contained the application checkout; a platform checkout
was created for the requested registry update. Neither checkout was deleted. The legacy repository
and its Golden Reference were not modified. This was a bounded repository/immediate-neighbor and
identified TEMP-artifact inventory, not an exhaustive scan of the user's disks or all TEMP files.
No production profile, window/tray state, font cache, actual backup/preset or autostart registration
was accessed or changed by cleanup.

## Validation record

Original checkout after removing old generated outputs: dotnet restore, dotnet build
--no-restore, and dotnet test --no-build --logger "console;verbosity=normal" all exited 0.
1,083 passed, 0 failed/skipped; build warnings/errors 0. SDK 10.0.401, Windows x64,
.NET/Windows Desktop runtime 10.0.12. The existing bootstrap control-flow suite separately
passed 18/18 isolated cases; it invoked no real installer or SDK command.

Committed fresh-checkout gate: f890046ea3da274258baef749099095fcd6e3956, created with
`git clone --no-local` into a unique TEMP directory and checked out detached. Initial
tracked/untracked status was clean and no Git object alternates were present. Only committed
files were copied; original build outputs, local scratch and prior native fixtures were absent.
README, CONTINUITY and DEVELOPMENT were read from this checkout, and their relative document
links were checked. The documented CLI restore/build/test sequence all exited 0:
**1,083 passed, 0 failed/skipped; build warnings/errors 0**. `git diff --check` passed and
the checkout remained clean apart from newly generated ignored build output.

This follow-up records measured evidence only; no source, test, dependency, SDK or bootstrap
change follows the verified handoff commit. Final main/remote identity is established by Git
push/readback rather than embedding a self-referential commit SHA in this file.
The fresh checkout used the existing Windows x64 SDK 10.0.401/runtime 10.0.12 installation
and normal NuGet cache/feed; it does not prove a blank Windows installation or empty package
cache. It does prove that no original working-tree scratch or AI session memory was needed.

A separate read-only review checked source/document agreement, retained future requirements,
Git history, links/anchors and new-path/secret exposure; no actionable findings remained.
New handoff documents and added lines contain no personal workspace paths, actual timetable
records or credential material detected by the targeted audit. Historical tool names/session
observations are evidence attribution, not dependencies for continuation. Source/tests and
font/license assets are unchanged from the implementation baseline.
The reference commands are in [Development](DEVELOPMENT.md). No native foreground run is required
for this documentation/chore milestone. Automated WPF object/event, fake-store and isolated
process tests do not establish native keyboard/IME, shell notification delivery, actual login
or every DPI/monitor behavior.

## Documentation and safety audit

README is a current landing page; CONTINUITY is the handoff; FEATURE-MAP contains current delivery
and remaining work; PROJECT-HISTORY links milestones to commits and evidence; the ADR index
provides every accepted decision. Architecture retains dated detailed evidence and labels it as
history. DEVELOPMENT supplies direct CLI and isolated native-run commands without personal paths.
Agent rules contain work policy and canonical links rather than unique product state.

No teacher/group implementation, profile v6, application behavior change or source duplication
is part of this milestone. The existing bootstrap's obsolete zero-product-tests message is removed.
Historical evidence retains its original methods/limitations; personal diagnostic paths in new
handoff documents are avoided. External raw screenshots/logs are supplemental, not prerequisites
for building or reading the recorded results. No new chat transcript or internal reasoning is stored.
