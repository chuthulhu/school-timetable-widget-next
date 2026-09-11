# Bundled and online font catalog

2026-09-11. IMPLEMENTED — automated verification passed; native UX approved by the user.
Authority: Bundled + Online Font Catalog milestone and explicit continuation for the preset-selection P2.
Start: main, HEAD/origin/main `26f6614d261873a551006bcb0f59c9d45abbf5cf`, clean.
[ADR 0016](adr/0016-bundled-and-downloaded-fonts.md) records the accepted behavior.

## User workflow

표시 설정에서 시간·날짜·요일·상태 각각의 **앱 제공 글꼴 / Windows 글꼴 / 온라인 글꼴**을 고릅니다.
앱 제공·Windows·이미 다운로드한 글꼴은 선택 즉시 미리보기 됩니다.
처음 쓰는 온라인 글꼴은 **다운로드**를 눌러야 적용되며, 그 전에는 이전 글꼴을 유지합니다.
성공하면 **사용 가능**으로 바뀌고 해당 요소에 즉시 미리보기 됩니다. 실패하면 오류와 재시도를 제공합니다.
새 온라인 선택을 다운로드한 뒤 파일이 사라졌다면 Apply는 재다운로드를 요구합니다.
이미 저장한 표시 또는 프리셋의 온라인 참조는 파일이 없어도 유효하며 기본 글꼴로 표시합니다.

Apply/OK는 표시와 프리셋 라이브러리만 기존 전체 profile 저장 transaction으로 커밋합니다.
Cancel/X는 마지막 Apply의 표시와 라이브러리를 복원합니다. 다운로드 완료 파일은 재사용할 수 있도록 남습니다.
진행 중 다운로드는 창 닫기/Reset/프리셋 전환으로 해당 Draft가 폐기되면 취소하며, 늦은 완료는 새 Draft를 덮지 않습니다.
임시 온라인 picker 후보는 canonical Draft 선택이 아니므로 다운로드 전 Apply/Save As는 이전 글꼴을 저장합니다.
이 상태는 picker 아래에 명시합니다. Reset은 선택한 사용자 프리셋 또는 기본 스타일의 payload를 복원합니다.

## Assets and provenance

Every font is an unmodified static desktop TTF. Regular faces are bundled for Pretendard and DSEG;
Orbitron uses the upstream Light face as its family default, IBM Plex Mono uses Regular.
WPF may synthesize unavailable weights/styles. No variable axes or WOFF/WOFF2 support is claimed.
[Microsoft WPF packaging guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/packaging-fonts-with-applications)
supports private resource/content TrueType/OpenType use without system installation.

The authoritative [machine-readable catalog](../src/SchoolTimetableWidget.Desktop/Assets/Fonts/catalog.json)
is embedded in the application. Its source, ID, family name, version, source commit, download URL,
exact size, SHA-256, license file and reserved name are not user profile data.

| Source / ID | Family / version | Upstream / exact source | Included file / SHA-256 |
| --- | --- | --- | --- |
| Bundled / `pretendard` | Pretendard / 1.3.9 | [orioncactus/pretendard](https://github.com/orioncactus/pretendard) · `5c41199ea0024a9e0b2cb31735265056e5472d76` · [asset](https://raw.githubusercontent.com/orioncactus/pretendard/5c41199ea0024a9e0b2cb31735265056e5472d76/packages/pretendard/dist/public/static/alternative/Pretendard-Regular.ttf) | `Pretendard-Regular.ttf` · `6d0af5258997aec7354a6e340fc2325ba321c410ca48b3af858c8c3d6e92a324` |
| Bundled / `dseg7-modern` | DSEG7 Modern / 0.46 | [keshikan/DSEG](https://github.com/keshikan/DSEG) · `a5019e1351dfa7b3c52aa3eff52ffb9c49538719` · [asset](https://github.com/keshikan/DSEG/releases/download/v0.46/fonts-DSEG_v046.zip) | `DSEG7Modern-Regular.ttf` · `ca80947f2676aba346c2b8cce37e05ba99e74df03a43ea4eb85bf5bbed531a42` |
| Bundled / `dseg7-classic` | DSEG7 Classic / 0.46 | [keshikan/DSEG](https://github.com/keshikan/DSEG) · `a5019e1351dfa7b3c52aa3eff52ffb9c49538719` · [asset](https://github.com/keshikan/DSEG/releases/download/v0.46/fonts-DSEG_v046.zip) | `DSEG7Classic-Regular.ttf` · `e3270928ced68082f32d3b62877a6741426116335ac0832919326341a8b5cfaf` |
| OnlineDownloaded / `orbitron` | Orbitron / 1.000 | [theleagueof/orbitron](https://github.com/theleagueof/orbitron) · `13e6a5222aa6818d81c9acd27edd701a2d744152` · [asset](https://raw.githubusercontent.com/theleagueof/orbitron/13e6a5222aa6818d81c9acd27edd701a2d744152/Orbitron%20Light.ttf) | `Orbitron-Light.ttf` · `c04a39535c1ab53cc667211fc4a46abe34641227993a05ad307fe14696c9d3a6` |
| OnlineDownloaded / `ibm-plex-mono` | IBM Plex Mono / 2.3 | [google/fonts](https://github.com/google/fonts) · `8e44913e4ff26fc997e6856c1ec40ff4791c98c5` · [asset](https://raw.githubusercontent.com/google/fonts/8e44913e4ff26fc997e6856c1ec40ff4791c98c5/ofl/ibmplexmono/IBMPlexMono-Regular.ttf) | `IBMPlexMono-Regular.ttf` · `6a3412f058c7d8dfd9170c41e85ade48e5156ecb89356110ca57a0a27734af46` |

Bundled binary files live under `src/SchoolTimetableWidget.Desktop/Assets/Fonts/<id>/` as WPF Resource items.
Online binaries are not part of the Desktop application. The exact Orbitron bytes also appear as a
license-accompanied test-only embedded fixture in `tests/.../Fonts/Orbitron-Light.ttf`; automated tests
use fake transport with these pinned bytes and require no Internet. This fixture is not an app bundle font.

## License verification

All five selections use SIL Open Font License 1.1. Before copying binaries, the official upstream
license and release paths were inspected; commits were independently checked with GitHub's commit API.
The licenses permit use, embedding and software-bundled redistribution, including commercial software,
subject to their conditions: retain copyright and license notices, keep the font under OFL, do not sell
fonts alone, and respect reserved names for modified derivatives. We do not modify/subset/rename fonts.
This is a record of the licenses supplied by the authors, not a separate grant of rights by this app.

| Font | Notice and reserved name | License evidence shipped in output/publish |
| --- | --- | --- |
| Pretendard | Copyright 2021 Kil Hyung-jin; RFN Pretendard | [Pretendard-LICENSE.txt](../src/SchoolTimetableWidget.Desktop/Assets/Fonts/Pretendard-LICENSE.txt), [upstream](https://raw.githubusercontent.com/orioncactus/pretendard/5c41199ea0024a9e0b2cb31735265056e5472d76/LICENSE) |
| DSEG7 Modern / Classic | Copyright 2017 keshikan in v0.46 release; RFN DSEG | [DSEG-LICENSE.txt](../src/SchoolTimetableWidget.Desktop/Assets/Fonts/DSEG-LICENSE.txt), [official release](https://github.com/keshikan/DSEG/releases/tag/v0.46) |
| Orbitron | Copyright 2009 Matt McInerney; RFN Orbitron | [Orbitron-LICENSE.txt](../src/SchoolTimetableWidget.Desktop/Assets/Fonts/Orbitron-LICENSE.txt), [upstream](https://raw.githubusercontent.com/theleagueof/orbitron/13e6a5222aa6818d81c9acd27edd701a2d744152/Open%20Font%20License.markdown) |
| IBM Plex Mono | Copyright 2017 IBM Corp.; RFN Plex | [IBMPlexMono-LICENSE.txt](../src/SchoolTimetableWidget.Desktop/Assets/Fonts/IBMPlexMono-LICENSE.txt), [pinned catalog license](https://raw.githubusercontent.com/google/fonts/8e44913e4ff26fc997e6856c1ec40ff4791c98c5/ofl/ibmplexmono/OFL.txt) |

The release DSEG notice text is retained, rather than replaced by the newer master copyright notice.
License files normalize line endings and trailing whitespace only; their notices and terms are unchanged.
TTF OS/2 fsType values inspected: Pretendard 0, DSEG 8, Orbitron 4, IBM Plex Mono 0. These are recorded
without modifying binary flags. The app loads private fonts for display; it does not export embedded-font
editable documents. The explicit OFL licenses above govern the included/downloaded distributions.
The Settings **글꼴 정보 / 라이선스** expander identifies version, license and source; complete notices
and catalog are copied to `Assets/Fonts` in both build and publish output.

## Ownership and security

- `Features/Fonts/FontCatalog`: immutable application-owned catalog. Canonical selection is
  `FontSelection(Source, Family, FamilyId)`. `Source + FamilyId` distinguishes same-name fonts.
  System uses the Windows family name as FamilyId; private selections require an exact catalog ID/name pair.
- `Infrastructure/Fonts/DownloadedFontCache`: `<profile-directory>/fonts/<id>/<source-commit>/<file>`.
  Production profile directory is `%LOCALAPPDATA%/SchoolTimetableWidget`; TEMP/dev profile selection also
  isolates fonts. A full source commit is the cache version key. Profile JSON contains no URL, binary,
  absolute path, provider metadata or cache inventory.
- `HttpsFontDownloadTransport`: explicit calls only, HTTPS, no redirects, fixed raw.githubusercontent.com
  URLs supplied by the static catalog. A 45-second bounded operation and 8 MiB streaming limit apply.
- Download commits use unique same-directory temporary files, complete bytes, flush/close, exact catalog
  size, sfnt signature and SHA-256 validation, then final same-directory move. Only owned temporary files
  are cleaned. A semaphore and exclusive per-version lock prevent corrupt concurrent writers; a conflicting
  external cooperating instance can fail/retry. No generic download queue or arbitrary URL/path entry exists.
- `Infrastructure/Windows/FontLibrary`: private WPF resource or validated local-file resolution, forced
  GlyphTypeface load and digit coverage check; Segoe UI/system message fallback. WPF's normal glyph fallback
  handles Korean absent from DSEG/Orbitron/IBM. No registry, elevation, system installation or process launch.
- App composes one FontLibrary for settings and header. Uncomposed test/view defaults have no production cache.
  Successful download refreshes header resource resolution even when the saved identity itself is unchanged.
  Font changes remeasure layout; ordinary clock ticks do not resolve/hash/enumerate fonts or use network.

Missing/corrupt cache falls back, leaves original canonical display and custom preset identities intact,
and does not invoke degraded profile mode. Cache download errors do not participate in unrelated timetable,
schedule/date/lunch saves. Catalog identities are validated separately from runtime availability; unknown
private IDs remain invalid profile values. Resource failures for known bundled IDs use rendering fallback.
No automatic startup download/catalog refresh, font cache cleanup UI, local import, arbitrary marketplace,
Google account/API/search, preset import/export or color editor is included.

## Schema v4

V3 font objects were strictly `{ source, family }` and permitted only System. V4 adds required `familyId`
and new source semantics in both active display and user preset payloads. A version bump is intentional;
older v3 readers would reject these fields/source kinds. Separate strict v1/v2/v3 readers preserve their
original shapes and inject System FamilyId in memory; no load rewrite or degradation occurs. The next
successful whole-profile save writes v4. No downgrade writer is offered. Atomic profile save is unchanged.

## Preset selector P2: root cause and correction

An unshown WPF reproduction recorded the original Save As sequence: session retained the newly created
Guid, but the selector removed its old item, received two ItemsSource Reset events (Load's all-property
notification followed by EditLibrary's PresetChoices notification), and ended with no selection.
The getter allocated a new array and item wrappers on every read. Reading SelectedValue during diagnostic
callbacks could make the failure disappear; a passive event trace reproduced it without intervening reads.
The passive trace was saved in TEMP `font-preset-binding-trace.txt`; the original failing test is unchanged.

Merely caching the collection and re-notifying SelectedValue did not solve the extended rename case:
replacing a renamed item caused two-way selection's transient null to interfere with source-to-target
selection refresh. The final implementation caches one choices snapshot per immutable library revision,
resolves the canonical preset reference to its matching current item, and binds SelectedItem one-way to
that resolved item. A view SelectionChanged handler submits only non-null selected IDs as user edits.
After library publication the matching selection is explicitly notified. No delay, fallback to first item,
exception swallowing, retries or weakened/skipped assertions are used. Preview/persistence ownership is unchanged.

Regression coverage checks SelectedItem and SelectedValue, exact ID/current-item membership, Save As,
collection refresh, rename, typography/Preview, Reset, no pre-Apply disk write, Cancel rollback,
Apply plus later edits, repeated built-in/user transitions and reopen. System/Bundled/Online use the same cases.

## Verification record

Initial font tests: 38 passed. The prior full suite exposed the documented selector failure, a schema
expectation requiring the approved v4 update, and a WPF PackagePart stream-list race under concurrent STA
resource tests. Font resource/render tests now run in a nonparallel xUnit collection, avoiding process-global
WPF package/font-cache probes racing unrelated view tests; no production threading/flags are changed.
After the P2 fix: original failing test passed three separate runs; related original/new WPF tests passed
three separate runs (7 each); full suite passed 801/801. Subsequent missing/corrupt-cache and availability-at-Apply
coverage is included in the final checks recorded below. These repetitions are bounded validation, not retries
that hide failures. Automated tests do not activate windows, inject input, change clipboard/system time or
access production profiles. WPF glyph-run/offscreen rendering is not claimed as native screen/IME evidence.

Automated validation, real-source integration and native UX acceptance passed. Evidence methods and limitations are recorded below.

### Pre-native final checks and real-source integration

- Restore/build and the full normal-verbosity run passed **804/804**, warnings/errors 0.
  Log: `%TEMP%/font-final-before-native.log`. The 44 added cases include the original 38 font tests,
  three source-specific selector transaction cases, and three availability/fallback cases.
- Production transport downloaded both pinned online files into an isolated integration directory.
  SHA/size validation and actual WPF GlyphTypeface family/file URI confirmed Orbitron and IBM Plex Mono.
  All three bundled GlyphTypefaces also loaded from pack resources. Both online families subsequently
  resolved with a transport that throws on any attempted network call. No network availability check is
  used for resolution. Integration log and disposable source: `%TEMP%/font-integration-1040f0b34c3f4cefb484adb0f7a139dd/`.
- Initial integration header bitmap captures were not usable for visual QA (bindings/background were
  not prepared for presentation); they are not counted as visual acceptance. Dedicated glyph-run/bitmap
  tests passed; actual native DSEG screenshot below separately showed the font.
- Release publish succeeded. Output contains all four complete license notices and catalog.json under
  Assets/Fonts; the three bundled binary SHA-256 values still match metadata. License normalization changes
  trailing whitespace/newline representation only, not copyright or terms. No online binaries are added
  to the Desktop publish output.
- Self-audit: canonical source/ID only, no profile URLs/absolute paths/binary/catalog inventory; no startup
  network or tick font I/O; HTTPS/no redirects/allowlisted assets/size/hash/temp/final rename; invalid cache
  never resolves; known missing fonts do not invalidate profile; Cancel retains complete cache; no system
  font install/admin/registry/external process/clock change; independent elements and custom IDs survive;
  strict v1/v2/v3 compatibility; tests and native use isolated TEMP; no excluded search/API/cache-manager scope.

### Native checkpoint and user acceptance

User authorized the foreground interval. Direct normal Debug app launch outside the sandbox used isolated
`results/native` beneath the integration directory, PID 94480, started 2026-09-11T16:04:57.8288000+09:00.
The app profile was prepared with a DSEG7 Modern Time / Pretendard Date / System Status user preset; online
cache was initially absent. Production profile/cache were not read or written.

Window discovery, screenshot and Ctrl+, succeeded. Native screenshot showed DSEG clock digits, Korean date
and current status without blank text; Settings showed the saved custom preset and independent font values.
The tool's window inventory omitted the owned modal as a separately targetable window. The parent capture
included it, but modal accessibility input reported an unavailable cached element; a coordinate probe
opened a source dropdown, while subsequent parent-target activation dismissed the dropdown before selection.
No successful online selection/download or Save As is claimed from those tool inputs. The remaining
actual modal workflow and UX were requested from the user in the already-running visible app.
The user replied "확인함" after the online download/immediate preview/Save As/save checkpoint.
Disk inspection found the actual user-selected combination Time = Bundled/DSEG7 Classic and
Date = OnlineDownloaded/Orbitron (not the initially suggested combination), saved as "교무실 시계",
ID `17119f33-12b2-4bb4-955f-e945fdca9e4a`. Native capture confirmed Classic clock and Orbitron date
with readable Korean fallback. Codex clicked the native title X; owned PID 94480 disappeared and stderr
was empty. No forced termination was used.

After final source hardening, restore/build again passed with warnings/errors 0, and the full suite passed
804/804 in 16.959 seconds (`%TEMP%/font-approved-final.log`). Cached restart PID 81528 showed the same
Classic/Orbitron combination; native X closed it, PID disappeared, stderr stayed empty. Profile SHA-256
remained `3F74C7D30B0FB82831344063845490F6A981CAF490C73F0213967E36545199E5`.

A separate TEMP `results/missing-92a248d2594d46448ecc18fe1dd9fcef` copied only that profile, without
font files. Owned PID 108720 started normally. Native capture showed readable fallback date, unchanged
Classic clock, and no degraded-mode notice. Opening Settings showed "내 프리셋 · 교무실 시계", Classic
Time and the original Orbitron Date identity still selected. Original profile/cache remained untouched.
The user confirmed re-download/immediate restoration and approved the complete font/preset UX with
"확인, 승인". A final native screenshot showed Orbitron date digits restored and the Classic clock unchanged.
Downloaded Orbitron SHA-256 matched the pinned catalog; profile SHA-256 remained the value above.
Codex clicked native title X, waited for owned PID 108720 to exit, and verified empty stderr. No forced
termination was used. All three owned native runs ended; no production profile/cache was accessed.

OS networking was not disabled;
network-independent cached resolution is established by the injected no-network integration/tests above,
not described as a host-offline native experiment.

### Post-approval final gate

After the user's final approval, `dotnet restore`, `dotnet build --no-restore` and
`dotnet test --no-build --logger "console;verbosity=normal"` passed: **804/804**, warnings/errors **0**,
15.3959 seconds. Log: `%TEMP%/font-post-ux-final.log`. The full suite passed repeatedly across the
final implementation; no ignored, weakened or retry-until-pass selector test was introduced.
Final source/license metadata and bundled sizes/hashes were rechecked. Self-audit found no outstanding
P1/P2 issue. Native evidence is limited to the observed desktop/user workflows described above, not
exhaustive IME, multi-monitor/DPI or host-offline coverage. All requested milestone gates are satisfied.
