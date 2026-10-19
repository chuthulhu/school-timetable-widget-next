# Windows laptop verification — 2026-10-19

## Scope and provenance

Verified application source: `5f90852e263b61ec44813ae925769b9e192e4398` (PR #2
development source). A new detached checkout was used on a Windows x64 laptop,
SDK **10.0.402**, winapp **0.7.1**. The normal profile did not exist and the app's
HKCU Run value was absent. These checks used Debug, `--highlight-preview` and
explicit unique TEMP `--dev-profile-directory` paths. Preview selects synthetic
timetable data and a repeating application clock; source inspection confirmed that
the NTP coordinator starts only outside preview. No system clock mutation or live
NTP probe was part of these checks.

The records below describe completed, dated executions. The documentation update
did **not** rerun build/tests or native checks. It changes no approved product
behavior. [Development policy](DEVELOPMENT.md#non-interfering-windows-verification)
and [ADR 0026](adr/0026-widget-appearance-and-window-interaction.md) remain applicable.

All times below are **KST (UTC+09:00)** on 2026-10-19. Machine names, account paths,
desktop screenshots, process inventories and synthetic profile bytes remain local;
only evidence filenames and scoped findings are recorded here.

## Automated execution

The selected SDK satisfied global.json. Commands ran in order from the fresh checkout:

```powershell
dotnet restore SchoolTimetableWidget.sln -p:CoreOnlyTests=false
dotnet build SchoolTimetableWidget.sln -c Debug --no-restore -p:CoreOnlyTests=false -v:normal
dotnet test SchoolTimetableWidget.sln -c Debug --no-build -p:CoreOnlyTests=false --logger "console;verbosity=normal" --logger "trx;LogFileName=full-windows.trx" --results-directory <local-evidence-directory>
pwsh -NoProfile -File .\tests\bootstrap-dev.Tests.ps1
```

- Restore succeeded. An initial sandbox build returned exit 1 with no diagnostic
  errors; its log was preserved. The authorized host-boundary build succeeded with
  **0 warnings / 0 errors**, completing by 16:40:25 (log modification time).
- Full Windows TRX: **1,294 executed, 1,294 passed, 0 failed, 0 not executed**.
  Recorded start **16:40:54**, finish **16:41:24**. This is not a Core-only count.
- Bootstrap: **18 isolated cases passed**, log completed by 16:43:25. These use
  stubs and do not invoke a real installer or SDK installation command.
- Local files: `restore.log`, `build.log` (initial failure), `build-host.log`,
  `test.log`, `test-results/full-windows.trx`, `bootstrap-tests.log`.

No claim is made of an empty global SDK environment or an installed end-user package.

## Native execution sequence

### First GUI session and targeting recheck — 16:41 onward

The first preview launched at 16:41:51. A window and UIA content were discoverable,
but initial native input returned `no_interactive_desktop`. Input stopped. After
the interactive desktop became available, actual inputs resumed around 16:50.
The standard and separate transparent synthetic profiles were exercised sequentially;
same-profile restart restored the saved synthetic cell and display/window preferences.
All three diagnostic processes had exited through the tray by 17:07:47.

The focused recheck began at 17:10:13 with one new synthetic profile. Initial
foreground activation was refused. Read-only point ownership showed that the tested
right-edge coordinates belonged to another process despite lying inside the widget's
rectangle; no input was sent there. Tray hide/restore brought the widget to the
foreground. Subsequent inputs used points confirmed to belong to the diagnostic.

| Condition | Measured outcome |
| --- | --- |
| Ordinary cell opacity 1, move lock off, verified left edge | Width 800 → 821 px |
| Ordinary cell opacity 1, move lock on, verified left edge | Width 821 → 801 px |
| Ordinary cell opacity 0, move lock on, verified first cell | Double-click opened the owned editor in the foreground |
| Ordinary cell opacity 0, move lock on, verified left edge | Width 801 → 781 px |

These observations supersede the earlier inconclusive resize/double-click results
within this scope. They are **not confirmed product defects**. The ownership of the
original failed attempts was not measured at the time, so their exact cause is not
retrospectively proved. The widget's bottom placement and other windows can affect
foreground/point ownership. No all-edge or all-z-order guarantee follows.

Evidence: `RESULTS-FINAL.md`, `TARGETED-RECHECK.md`, `targeted-recheck.json`,
`standard-target-points.json`, `locked-target-points.json`,
`transparent-target-points.json`, `resize-left-unlocked.json`,
`resize-left-locked.json`, `resize-transparent-locked.json`,
`transparent-doubleclick-windows.json`, `target-check-final.json`.

### Second GUI session: IME and actual screen composition — 21:26–21:30

One new Debug preview ran **21:26:07–21:30:25**, at **96 DPI / 100%**. Its synthetic
profile started from the previous transparent fixture. Target foreground and sampled
cell coordinates were verified before input. A header point owned by another process
was not clicked. Transparent-cell double-click opened the editor again.

Actual Korean composition was attempted using the existing Korean keyboard layout,
raw R/K/S/K virtual keys, an app-local Hangul key and right Alt. Observed strings
remained Latin (`rk `, `rksk `, then `rksk rksk `); selection replacement also failed
in the last attempt. Key-transport acknowledgement is not IME success. The checks
cannot distinguish tool/IME routing limits from an application issue. No input method
was installed and no system input settings changed. The trial was cancelled and the
original synthetic cell text was observed again. **Actual Korean IME composition
remains unverified**, distinct from previously successful Unicode text injection.

For composition evidence, `winapp ui screenshot <cell-selector> --window <widget>
--capture-screen` captured only the same **144×82 px** first-cell screen region:

- `actual-transparent-cell.png`: at ordinary-cell opacity 0, desktop shortcut icons
  were visible behind the synthetic text. Actual desktop composition worked, but
  black text overlapping dark background/icons was difficult to read.
- `actual-opaque-cell.png`: after changing only ordinary-cell opacity to 1 through
  Display Settings and saving to this synthetic profile, the same region had a white
  background and legible Korean text.

This is actual screen-region evidence, unlike the earlier window-only captures whose
transparent regions appeared black. It is a scoped readability observation, not a
quantitative alpha/color test or acceptance for every background. No desktop captures
are included in the repository. The comparison used custom opacity values, **not**
the built-in Transparent preset. Evidence also includes `LOCAL-CHECK-2126.md`,
`LOCAL-CHECK-2126.json`, `ime-screen-launch.json`, `ime-screen-composed.json`,
`ime-screen-rightalt-result.json`, `ime-screen-final.json`.

## Consolidated 19-item checklist

“Confirmed” below applies only to the stated conditions. This is not 19/19 acceptance.

| # | Item | Evidence and remaining limit |
| --- | --- | --- |
| 1 | Header opacity | Synthetic fixture observed; exact alpha/visual comparison not completed. |
| 2 | Ordinary-cell opacity | UI 0→1 change and actual screen comparison confirmed; opacity 0 had poor contrast over dark icons. |
| 3 | Current-period opacity | Synthetic highlight observed; exact opacity/transition combinations not fully checked. |
| 4 | Border opacity | Fixture/capture evidence only; quantitative visual check incomplete. |
| 5 | Independent typography | Cell Nanum Gothic/20 saved while header Segoe UI/15 remained; no online download. |
| 6 | Preview/cancel | Seconds preview changed immediately and Cancel restored seconds; cell trial cancellation restored content. |
| 7 | Frameless/transparent cells | Frameless widget and targeted transparent-cell double-click confirmed; not every transparent region tested. |
| 8 | Unused transparent margins | Click-through test not performed; points owned by other processes were not clicked. |
| 9 | Body drag | Unlocked synthetic widget position moved from (40,40) to (135,88). |
| 10 | Double-click/F2 | Standard editing/F2 and targeted transparent-cell double-click confirmed. |
| 11 | Korean input | Unicode injection/save worked; actual IME composition remains unverified after raw-key attempts. |
| 12 | Button/scroll/resize conflicts | Settings scroll/buttons worked; complete interaction combinations not tested. |
| 13 | Resize with movement lock | Verified left-edge size changes with lock off/on, including opacity 0; other edges not comprehensively tested. |
| 14 | Numeric size/errors | Invalid `abc` produced a range error and retained input; 900 DIP preference applied/restored. |
| 15 | Focus/z-order | Owned dialogs, Tab focus and targeted foreground confirmed; other-app/z-order combinations incomplete. |
| 16 | Dialogs | Display, window settings and cell editor exercised; native file dialogs not tested. |
| 17 | Tray/save/restart | Synthetic save/restart, tray hide/restore and normal exit confirmed in the first session/recheck; second session saved display and exited, without another restart. |
| 18 | Standard/transparent comparison | Separate synthetic fixtures and same-cell screen 0/1 comparison; built-in Transparent preset selection untested. |
| 19 | DPI | 96 DPI / 100% only. **150% and 200% unverified**; system scale was not changed. |

## Cleanup and practical limits

Every diagnostic was exited via its tray menu and process absence was checked. The
normal profile remained absent and the app's HKCU Run value remained absent. No real
timetable import/restore, autostart change, source modification, remote push, merge,
deployment, system scaling change or reboot occurred. Local evidence and synthetic
profiles were retained outside the repository. File upload failure is not a runtime
test failure; no successful external attachment is claimed.

The supported conclusion is **buildable and runnable with scoped native checks on
this laptop**. Actual IME, higher DPI, full dialog/interaction coverage and transparent
theme readability still require appropriate follow-up. No official installer or
complete production-readiness acceptance was produced.
