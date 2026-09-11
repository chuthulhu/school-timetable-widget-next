# Display Settings / Presets Foundation

Date: 2026-09-11. **IMPLEMENTED — NATIVE REVIEW CONFIRMED**.
Authority: user milestone request; [ADR 0014](adr/0014-display-presets-and-schema-v2.md).
Start: main, HEAD/origin/main 45418fe31aade3634419946837e98b28a3ba5c86, clean.
No branch switch/reset or remote/system-setting change.

## User behavior

시간표 또는 시계 영역을 우클릭해 **표시 설정...**을 열거나 **Ctrl+,**를 사용합니다.
표시 스타일을 고른 다음 시간·날짜·요일·상태의 글꼴, 글자 크기, 굵기, 기울임을
각각 바꿀 수 있습니다. 변경한 내용은 메인 화면에 즉시 미리 표시됩니다.
적용은 저장하고 창을 유지하며, 확인은 저장 성공 후 닫습니다. 취소/X는 마지막으로
적용한 설정으로 돌아갑니다. 기본값으로는 현재 선택한 표시 스타일만 초기화해 미리 봅니다.

| Style | Initial layout / values |
| --- | --- |
| 표준 | Date, time, full status on one row; 16 / 16 / 16; 24-hour with seconds |
| 디지털 | Centered large time (48), separate date (14) and full status (15) rows |
| 컴팩트 | Shorter one-row time (16) and full status (14); date and seconds initially hidden |
| 미니멀 | Time (24); date, status and seconds initially hidden |

All presets initially hide weekday and use Segoe UI, normal style. Digital/Compact time
starts at medium weight. These are editable initial values, not font restrictions.
Compact retains full status/countdown semantics; the user's abbreviated example is not
a new Core status mode. Enabling date/weekday/status works in every layout independently.
12-hour format has Korean 오전/오후, midnight/noon 12 and unpadded 1–11.
Date remains yyyy년 MM월 dd일. Column M/d and short weekday labels do not use these settings.
Time size 10–96, supporting sizes 8–48, finite values only, in WPF device-independent units.
Weight choices are 얇게/보통/중간/굵게; style choices 보통/기울임.

System-installed family names are selectable, including user-installed families that WPF
enumerates. An editable picker retains missing names and shows a short fallback notice.
The Windows adapter resolves locally to Segoe UI or the system message font if missing.
Availability never changes the canonical family identity or makes a profile invalid.

## Ownership and safety

- DisplayConfiguration is immutable Desktop data, separate from WPF, Core and JSON DTOs.
  Preset, layout and each element's typography are independent. FontSelection explicitly
  names its source; new sources can be introduced without a provider hierarchy.
- RuntimeDisplaySettings owns committed/current preview and one active display session.
  DisplaySettingsSession owns replaceable element Drafts and last successful Apply baseline.
  Invalid fields block Apply and retain the last valid preview; errors are user-facing Korean.
  Failed save retains old disk/committed/baseline and editable Draft/preview.
- ProfileSession saves the latest complete candidate before publishing commitment. Every
  other feature save retains committed Display, never current Preview. Display Cancel does
  not undo successful timetable/schedule/date/lunch saves.
- Formatter output remains independent CurrentDateText/CurrentTimeText/StatusText. Weekday,
  AM/PM and 12-hour text derive from the same existing snapshot. The ViewModel reformats
  cached facts on preview; no clock read, timer, tick font enumeration or persistence.
- The Header view retains its controls. Explicit configuration changes reserve fixed text
  slots/height and remeasure WindowContentMinimum; ticks do not reconstruct layout.
  Digital lines center inside stable slots. Oversized width uses horizontal scrolling.
  Normal resizing remains available. Full persisted preferred geometry is not implemented.
- Week arrows use a local rounded hover/pressed/focus template with ‹/›, no icon package.
  Dates have stronger visual weight than weekdays; Today changes background only.
  Current body highlight is unchanged, with no geometry-affecting trigger.

## Persistence v1/v2

Schema 2 stores required profile.display: preset, layout, time/date/weekday/status
(font source/family, size, weight, style), use24Hour, showSeconds, showDate, showWeekday,
showStatus. Canonical values use names; paths, unknown enums, missing/null/extra fields
and invalid sizes are rejected safely. No derived current text or viewed week is stored.

A separate strict v1 DTO reads the previous exact shape. All timetable strings, tick-precision
period times, independent date overrides and lunch survive; Standard defaults are injected.
v1 load remains writable and never rewrites the file. Any next successful user save writes
v2. The atomic write/lease/external-change protection is unchanged. There is no downgrade
support; old v1-only apps may treat v2 as unsupported.

## Verification scope

Automated tests cover preset defaults, independent overrides/reset, formatting including
midnight/noon, options, invalid input, missing fallback and no extra clock reads; P2 preview,
Apply/OK/Cancel, failed-save retry, one-editor ownership and cross-feature preservation;
fixed v1 fixture exact load/no rewrite, next-save upgrade, v2 round trips, strict invalid
display rejection, missing-font portability and all four atomic write fault stages.

Unshown WPF object/event tests cover selectors, actual binding changes, independent controls,
preview, buttons/X closure, layout switches, visibility/font properties, command routing,
arrow styles, extreme sizes and unchanged header/35-cell geometry across ticks/resizes.
Existing Today/highlight/week navigation and original regression tests remain.
These are object/event/layout observations, not actual keyboard/IME/OS/native UX evidence.

Temporary offscreen RenderTargetBitmap images are used for layout inspection only. They
do not establish inactive/occluded/native equivalence. No native input, pointer movement,
clipboard change or production profile access is used by automated verification.

Final command results and native launch/user acceptance are appended below when observed.
Commit and fast-forward origin/main push require user native UX approval first.

## Deferred

| Feature | Status |
| --- | --- |
| Display Presets | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| Per-element typography | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| System font selection | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| Bundled font catalog | PLANNED |
| Online font catalog/download/cache | PLANNED |
| Named custom presets | PLANNED |
| Color/theme editor | PLANNED |

Local font file import, independent Title/AM-PM controls, final Fluent redesign and whole
responsive layout framework are also not implemented. No new dependency or font download.

## Native review checklist

Codex directly launches the normal app with a unique TEMP profile after automated validation.
Confirm actual visibility, Standard familiarity, Digital emphasis, Compact height, Minimal
balance, immediate preset preview, system font selection, independent element sizes,
12/24-hour/seconds/visibility, Cancel and last-Apply restoration, Apply/restart retention,
arrows, Today and resize. Codex observes owned process lifecycle and persisted data.
Human UX approval is pending; no native success or commit/push is claimed before it.

## Automated validation and self-audit — 2026-09-11

- dotnet restore and dotnet build --no-restore exited 0; build warnings/errors 0.
- dotnet test --no-build --logger "console;verbosity=normal" exited 0:
  **695 passed, 0 failed/skipped** (baseline 623 plus 72 new cases).
- Log: C:/Users/ADMIN/AppData/Local/Temp/display-final-6e30524dcf9c4354ba562bcf0cef000a.log.
- Fixed embedded profile-v1.json verifies original v1 shape independently of the current
  serializer. Full original JSON inputs survive in-memory load and next-save v2 upgrade;
  startup bytes/mtime stay unchanged and the profile remains writable.
- Earlier baseline expectation changes only reflect approved v2 schema, added display
  properties/menu, new chevrons and the configurable header containers. No original test
  was removed/skipped; clock counts, data safety and geometry assertions remain.
- Final source/transaction/scope self-audit found no outstanding P1/P2 finding in the
  inspected scope. Independent element inputs, layout/typography separation, logical font
  identity/fallback, P2 rollback/failure, all-feature save preservation, strict v1/v2,
  atomic-write regression and tick stability were checked. No extra clock read, Core
  display model, downloaded/bundled font, custom preset manager or global Settings state.
- Temporary offscreen inspection found the four layouts distinguishable and informed
  Digital text centering. Rendering is unshown WPF evidence, not native acceptance.
  Images: C:/Users/ADMIN/AppData/Local/Temp/display-review-68221bcae50b4e3eb460aac5652c9c2d.
- A final whitespace check initially found added EOF blank lines. They were removed;
  touched text files were normalized to the repository's Windows line endings.
  git diff --check subsequently exited 0. This changed no program behavior.
- Native review and human UX approval are still pending. No commit/push performed;
  main HEAD remains 45418fe31aade3634419946837e98b28a3ba5c86.

## Direct native launch — awaiting visibility confirmation

The normal Debug app was directly launched outside the sandbox at
2026-09-11T12:15:22.1139255+09:00. PID 64984, expected executable/title, input-idle true,
running with window handle 2494506; stderr length 0. These observations establish startup,
not actual visibility on the user's desktop.

TEMP profile: C:/Users/ADMIN/AppData/Local/Temp/display-native-87c37e43d83f4e5d80f716057d0b3d7b.
Arguments: --timetable-preview and that --dev-profile-directory. This uses sample timetable
data with the normal shared PC fallback clock; no synthetic clock or altered window flags.
No production profile, system settings, native keys/pointer or clipboard were changed.
The user must confirm actual window visibility and the requested native UX; restart/normal
closure and saved display retention will then be observed using this same isolated profile.
Native approval, final post-approval verification, commit and push remain pending.

### Actual desktop visibility confirmed

The user answered "보임", confirming the directly launched app is visible on the actual
Windows desktop. Codex rechecked PID 64984, exact executable/start timestamp and responsive
state; stderr remains empty. The diagnostic reader preserves the JSON timestamp as a string
to compare the recorded identity without PowerShell's automatic date conversion.

This confirms visibility for this launch only. Display preset readability, native interaction,
restart retention and overall UX approval are still pending.
## Direct computer-use verification — 2026-09-11

The user explicitly requested direct computer control. During that authorized foreground
interval, Codex used computer-use 26.903.71938 / @oai/sky against the normal app and the
same isolated TEMP profile. Mouse/keyboard input was used in this interval; the earlier
background-only verification description does not describe these native steps.

Observed native successes:

- Ctrl+, opens the owned display settings dialog. Standard header and the 35-cell grid
  render with Today and the current period highlighted; maximization renders the full grid.
- 24-hour, seconds, date, weekday and status checkbox clicks update the header immediately.
  Weekday can remain visible independently of the date. Cancel restores all original
  Standard values. Before Apply, profile.json remains absent after this preview/cancel run.
- Next week shows September 14–18 and removes Today/current-period highlighting. Previous
  week restores September 7–11 and Friday/current-period highlighting.
- Apply with seconds disabled persists schema v2 and leaves the dialog open. Re-enabling
  seconds as an unsaved preview and closing the dialog with title-bar X restores the last
  applied seconds-disabled state. The saved profile still contains all 35 timetable cells.
- Main title-bar X normally closes PID 64984; Codex independently verifies process absence
  and empty stderr. No forced termination is used as evidence.
- Restart of the same executable/profile at 12:28:05 +09:00 creates PID 64972. The header
  and reopened settings retain seconds disabled. Profile SHA-256 is unchanged on startup:
  1C0C94F4BD9C9D3A903DD82CAE59753CF77993EE6EAA3ACC763EB95500A948CC.
- Reset restores Standard defaults in the preview. OK saves seconds enabled and closes the
  settings dialog. Restart stderr remains empty. The restarted main window is left open
  for the user's review, using only the diagnostic TEMP profile.

Tool limitations and remaining evidence:

- The owned modal is captured and appears in the accessibility tree, but list_windows and
  list_apps expose only the main window as a target. Modal element click/set_value returns
  'element ... is not available in cached app state'. Screenshot checkbox/button clicks work.
- The preset dropdown opens and visibly lists all four presets. A subsequent popup click
  or arrow key targeting the only exposed main window closes the popup without changing
  Standard. Input activates the main window; this also changed its placement/restore state
  during one maximized-dialog attempt. These are observed automation limitations, not an
  established application defect. No fabricated modal target or changed production flags
  were used to bypass them.
- Native preset selection, per-element font/size typing, keyboard/IME and focus behavior
  remain unverified. Existing object/event tests and offscreen preset renders cover their
  stated scope only; they do not substitute for these native behaviors or human UX approval.

Human UX approval is still pending. No commit or push has been made. The source behavior
has not changed during this native run; only the verification record has been updated.
## User native confirmation and final acceptance — 2026-09-11

The user confirmed: "프리셋 선택·글꼴/크기 입력의 실제 조작 확인함".
This closes the remaining preset-selection and font/size native-operation review items
identified in the direct computer-use report. Together with the preceding native smoke,
this is recorded as the user's UX acceptance for this milestone. These specific interactions
are user-reported evidence, not successful Codex automation. No broader keyboard/IME or
focus-loss coverage is inferred. Earlier pending statements above describe historical stages;
this section and the current status tables supersede them.

The post-acceptance build initially encountered MSB3026/MSB3027/MSB3021 because the owned
TEMP review app still held its executable open. Codex verified PID 64972's executable and
exact start time, requested normal closure with Process.CloseMainWindow, and observed exit
within 10 seconds with empty stderr. This process API closure is not additional native-X
evidence. No forced termination or unrelated process action was used.
Final post-acceptance verification passed: dotnet restore, dotnet build --no-restore,
and dotnet test --no-build --logger "console;verbosity=normal" all exited 0. Build warnings
and errors: 0. Tests: 695 passed, 0 failed/skipped; 72 new cases over the 623-test baseline.
Log: C:/Users/ADMIN/AppData/Local/Temp/display-approved-final-235bc3728b5a4b479c96787cfb24b894.log.
The final self-audit revisited P2 success/rollback boundaries, preserved profile data, strict
v1/v2 DTOs, system-font fallback and same-snapshot display rendering; no outstanding P1/P2
finding was identified within this inspected scope. git diff --check passed. Bundled/online
fonts and the other explicitly deferred capabilities remain planned. Commit and ordinary
fast-forward push are authorized by the original request after this accepted verification.
