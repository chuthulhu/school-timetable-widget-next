# Feature Map

새 제품의 간단한 tracking 문서다. [Product Contract v0.1](PRODUCT-CONTRACT.md)이 authoritative 계약이며,
아래 상태는 구현 진행 상태다. `PLANNED`는 승인된 방향의 구현 전 상태이고,
`DEFERRED`는 제공 범위나 상세 설계 결정을 남긴 상태다. 구현 또는 검증 완료를 뜻하지 않는다.
PLANNED 기능에도 계약에 명시된 DEFERRED 상세는 그대로 남아 있다.
`PARTIAL`은 명시된 기반만 구현했으며 전체 기능 완료가 아닌 상태다.

| Feature | 상태 | Product Contract ID / section | 관련 ADR / 남은 상세 |
| --- | --- | --- | --- |
| Repository / Skeleton | IMPLEMENTED — Phase 0 skeleton + dev bootstrap | Architecture / Development Setup | [0005](adr/0005-phase-zero-project-structure.md); 3 projects, CLI runner 구성. 제품 기능/native 검증 완료를 뜻하지 않음 |
| Timetable Core/read-only model | IMPLEMENTED — FOUNDATION | A2, M1, I6–I7, R1–R3 | Immutable 35슬롯과 SubjectText/ClassText value, 완전성/중복/null 검증, period/day 정렬, 한 cell pair 교체와 문자열 보존; 저장 schema 아님 |
| Weekly Timetable read-only View | IMPLEMENTED — USER NATIVE SMOKE PASSED | A2, M1, I6–I7, A4, P3 | 별도 feature VM/View, 7×5 body ItemsControl, 숫자 1–7 교시 열, plain text/Wrap/Center, 측정 기반 minimum; 대표 표시/live update/가로 resize/X 종료 사용자 확인 |
| Weekday Header | IMPLEMENTED — USER NATIVE SMOKE PASSED | M1, A4; Golden Reference visual evidence | 월/화/수/목/금, 빈 corner + 왼쪽 숫자 교시 header와 함께 총 13 headers; Status Header 아래 |
| Period current-resolution foundation | IMPLEMENTED — FOUNDATION / Phase 0.3 | M3, P4, R4, R6–R7, I16 | Core immutable 정의·기본 profile·snapshot 기반 int? 계산, 평일 [start,end), 입력 중복/겹침 거부, contract tests. 전체 editor validation은 DEFERRED |
| Current Status Core foundation | IMPLEMENTED — FOUNDATION / Phase 0.4 | A6, A5, P4; Current Status State Model | 5상태 immutable 사실 결과, snapshot 기반 계산, 공통 schedule 검증. Break와 다음 교시/transition 구현; 전체 UI 완료 아님 |
| Current Highlight integration | IMPLEMENTED — USER NATIVE SMOKE PASSED | M4, R4–R5, I9, I16 | CurrentStatusRefreshLoop의 동일 snapshot/status → logical day/period slot → IsCurrent; empty/whitespace 포함 1 또는 0셀; native Break/수2 whitespace 화면과 조건부 style 수용 확인 |
| Highlight layout invariance | AUTOMATED / OBJECT VERIFIED + USER RESIZE SMOKE PASSED | R5, I9, I15 | Background-only DataTrigger, 전체 셀 DesiredSize/배치/wrapping/border 불변; 사용자 가로 resize 정상 확인, 전 DPI native 계측 아님 |
| Highlight final theme/settings | DEFERRED — Settings milestone | M5, P2 | 향후 커스텀 전제로 현재 후보 style 사용자 수용; 최종 색/opacity/설정 UI 미구현 |
| Upcoming highlight | DEFERRED — 미구현 | A4 Current Status Header | optional 보조 강조; current와 구분 유지 |
| Break status Core | IMPLEMENTED — FOUNDATION / Phase 0.4 | A6, P4; Current Status State Model | CurrentStatusResult의 Break, NextPeriodNumber, TransitionTime으로 구현; 긴 gap도 Break |
| Countdown Core foundation | IMPLEMENTED — FOUNDATION / Phase 0.5 | A7, A5, P4, I16; Countdown Display Semantics | 동일 snapshot/status의 tick 차이, floor·LessThanMinute·hours/minutes 의미 값, stale input 거부; UI 미구현 |
| Countdown presentation formatter | IMPLEMENTED — FOUNDATION / Phase 0.6 | A8, A7; Current Status Header Presentation Text | Desktop의 한국어 formatter 및 immutable 독립 텍스트 결과 (후속 CurrentDateText 포함), countdown 조합 검증, presentation contract tests. 다국어 infrastructure는 현재 범위 밖 |
| Current Status Header | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A4, A7, A8, A9, I15–I16; Current Status Header | 별도 UserControl의 텍스트 binding (날짜 추가 검증은 아래 별도 기록), 고정 높이 및 App activation 구현; 사용자 host Windows에서 표시/live update/기본 폭 및 가로 resize 안정성 확인 |
| Header ViewModel foundation | IMPLEMENTED — FOUNDATION / Phase 0.7 | A8, A9, I15–I16 | 세 read-only 문자열(CurrentDateText 포함), 계산 결과 Apply, 같은 문자열 PropertyChanged 억제; clock/timer/layout 책임 없음 |
| Live refresh loop foundation | IMPLEMENTED — FOUNDATION / Phase 0.7 | A9, A5, I16 | Desktop DispatcherTimer 약 1초, Start 즉시 refresh, cycle당 snapshot 1회, Start/Stop/Dispose, missed tick replay 없음 |
| Actual Header XAML/rendering | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A4, A8, I15 | XAML/binding/object tests 통과; 고정 높이/fixed time column/Tabular/NoWrap 후보. 기본 폭의 clipping/겹침 없음과 가로 resize 안정성 사용자 확인; font glyph 지원/DPI/최소 폭은 별도 검증 |
| Actual app activation/wiring | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A9, A5, I16 | App OnStartup에서 동일 clock/default schedule 주입, Start 후 Show, OnExit Dispose. 사용자 live refresh 확인 및 X 종료 후 process 소멸 확인; 계측된 cadence/Dispose 실행 증거는 아님 |
| Tray visibility refresh lifecycle | DEFERRED | A9, P6 | hide/show에 따른 loop Start/Stop 정책 미결정 |
| Suspend/resume integration | DEFERRED | A9, A5 | OS detection/event 연결 미구현; RefreshNow로 현재 상태 재평가 가능한 기반만 제공 |
| Application Clock / KRISS | PARTIAL — Phase 0.2 foundation | A5, I16–I20 | [0004](adr/0004-application-time-source.md); Core snapshot/interface, Desktop PC fallback, App 소유 경계, Tests fake/contract tests 구현. KRISS 동기화는 미구현; endpoint/client/보정/동시 전환 DEFERRED |
| Timetable Editing | PARTIAL — EDITING FOUNDATION COMPLETE / USER NATIVE SMOKE PASSED | M1–M2, A2, R16, I3, I6–I7; approved Editing Foundation | [0006](adr/0006-single-cell-in-memory-editing.md); 교과/반 별도 Draft, atomic one-cell in-memory Apply/Cancel; persistence 미구현 |
| Base Period Schedule Editing | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED | M3, P4, R6; approved base editing | [0009](adr/0009-editable-base-period-schedule.md); 정확히 1..7 chronological order, HH:mm Draft/validation, atomic Apply-and-close, persistence 제외 |
| Settings | PLANNED | P2–P3, M5, R8–R14, I2, I10, I13–I14 | [0003](adr/0003-settings-transaction.md); overflow/Reset UI 상세 DEFERRED |
| Persistence | PLANNED | R15–R21, I1, I3–I4; Data Safety Principles | [0003](adr/0003-settings-transaction.md); technology/schema/durability DEFERRED |
| Legacy Migration | PLANNED | P8, C1–C6, I4–I5; Migration Contract | [0001](adr/0001-golden-reference-policy.md); provenance/concurrency 상세 DEFERRED |
| Backup / Restore | PLANNED | P9, C6, R17–R21, I4 | [0003](adr/0003-settings-transaction.md); format/manifest/recovery 상세 DEFERRED |
| File Sharing | PLANNED | A3, P10, C3, C7; Sharing Scope | [0001](adr/0001-golden-reference-policy.md); 새 format 및 Legacy envelope 지원 상세 DEFERRED |
| Window / DPI / Multi-monitor | PLANNED | P3, P6, M6, I8–I9, I14 | [0002](adr/0002-windows-desktop-stack.md); close/z-order/monitor 제거/overflow DEFERRED |
| Tray | PLANNED | P6; Window / Tray Contract | [0002](adr/0002-windows-desktop-stack.md); Windows adapter 검증 필요 |
| Single Instance | PLANNED | P6, R23 | [0002](adr/0002-windows-desktop-stack.md); 사용자/profile당 writer 하나 |
| Autostart | PLANNED | A1, P7, C1, I11 | [0002](adr/0002-windows-desktop-stack.md); OS registration 상세 DEFERRED |
| Notifications | PLANNED | P5, C5, R24, I11, I16 | [0002](adr/0002-windows-desktop-stack.md), [0004](adr/0004-application-time-source.md); delivery/빈 수업 판정 상세 DEFERRED |
| Installer | DEFERRED | A1, P1; Installation / Lifecycle | [0002](adr/0002-windows-desktop-stack.md); 설치형 방향 승인, technology/배포 상세 DEFERRED |
| Updater | DEFERRED | R22; Installation / Lifecycle | [0002](adr/0002-windows-desktop-stack.md); 제공 범위/library/정책/서명·rollback DEFERRED |

이 파일은 Legacy evidence 문서의 복사본이 아니다. 과거 기능의 증거가 필요하면
[Legacy Reference](LEGACY-REFERENCE.md)의 고정 commit 문서를 읽는다.

## Future requirements — 2026-09-10

사용자가 승인한 future constraint이며 이번 Editing Foundation 구현에는 포함하지 않는다.

| Feature | 상태 | 요구 / 경계 |
| --- | --- | --- |
| Date-specific timetable override | FUTURE REQUIREMENT — NOT IMPLEMENTED | DateOnly 날짜의 예외; 기본 weekly 불변 |
| Date-specific period schedule override | PLANNED | DateOnly 날짜의 예외; 기본 schedule 불변; timetable override와 독립 |
| Effective day configuration | FUTURE REQUIREMENT — NOT IMPLEMENTED | timetable only / schedule only / both / neither; 같은 snapshot 날짜로 resolve한 구성을 Status/Highlight/Notification이 공유 |
| CurrentDateText | IMPLEMENTED — FOUNDATION / AUTOMATED + OBJECT VERIFIED | yyyy년 MM월 dd일; CurrentTimeText(HH:mm:ss)/StatusText와 독립; 동일 snapshot의 date/time/status/highlight; ADR 0008; native 날짜 가독성/resize 승인 범위는 PERIOD-SCHEDULE-EDITING 기록 |

모델/override UI/schema/calendar/date selector는 후속 milestone에서 설계한다.
소유권과 coupling 제약은 [Architecture](ARCHITECTURE.md#future-date-configuration-constraints--2026-09-10)에 기록한다.

## Bulk Timetable Input — consolidated future scope

현재 Editing Foundation은 SubjectText/ClassText 직접 한 셀 편집이다.
아래 기능은 **구현하지 않았으며** 상세 요구는
[Architecture future bulk scope](ARCHITECTURE.md#future-bulk-timetable-input--consolidated-2026-09-10)에 통합한다.

| Feature | 상태 | 방향 / 경계 |
| --- | --- | --- |
| Bulk Timetable Input | FUTURE / PLANNED | 명시적 A/B/C modes; format recognition 분리; 공통 parse/validate/preview/atomic Apply pipeline 가능 |
| School Timetable Import (A) | PLANNED — preferred bulk import UX | Metadata 앞뒤 허용; 1–7 × 5/35-slot structural signature와 optional weekday header; absolute column/semantic guessing 금지; 교과/반 row pairs; multiple teacher 후보 사용자 선택 |
| Canonical Template Import (B) | PLANNED — deterministic fallback | 정확한 8×11 표; 교시 + 월~금 교과/반 10열 exact headers와 1–7 validation; 시간표 양식 복사 TSV → spreadsheet A1 → 전체 복사 → Preview/Apply; .xlsx export는 별도 편의 기능 |
| Small Rectangular Paste (C) | OPTIONAL / PLANNED | Selected cell anchor, explicit Ctrl+V mode, 5-column subject / 10-column pair 후보; A/B와 heuristic 혼합 금지; range overflow reject |
| Date-specific import target | FUTURE / depends on Date Override milestone | Format/parser와 기본 weekly 또는 DateOnly 예외 target 선택 분리; parsed values/preview/atomic pipeline 재사용 |

모든 mode는 가능한 clipboard semantics 범위의 empty/Unicode/newline/whitespace 보존,
quoted/embedded newline 조사, Preview와 명시적 atomic Apply를 따른다. Parse/validation/
ambiguity/range 실패는 no modification이며 silent clipping/guessing/partial update 금지다.
현재 parser/template/clipboard/Bulk UI/DateOverride/calendar/provider hierarchy는 추가하지 않는다.

## Future teacher profiles and groups — 2026-09-10

현재 Editing Foundation 구현 범위를 확대하지 않는다. 모두 **FUTURE / NOT IMPLEMENTED**다.

| Feature | 요구 / 경계 |
| --- | --- |
| Teacher timetable profiles | Stable ProfileId + DisplayName + WeeklyTimetable 상위 소유 개념; 이름을 key로 사용하지 않음; WeeklyTimetable 자체에 teacher/group 추가 금지 |
| Groups and teacher tabs | 3학년 담임/과학교사/자주 확인하는 교사 등; group은 profile references만 보유, data 복제 없음; 동일 profile의 여러 group 소속 허용 |
| Multi-teacher school import | 여러 detected row-pairs에서 한 번에 여러 profile 선택 생성/갱신 가능성을 고려; 구현/target mapping UX는 후속 milestone |
| Profile-specific date overrides | 교사별 특정일 수업 변경과 학교 특정일 effective PeriodSchedule은 독립 concern |
| Effective selected-profile configuration | 동일 clock snapshot/date의 selected profile effective timetable + effective schedule을 Header/Highlight 등에서 공유 |
| Multi-profile persistence | 앱에 weekly 하나만 존재한다는 schema/coupling 금지; 현재 persistence 없음 |

TeacherTimetableProfile/TimetableGroup 모델, multi-tab UI, multi-teacher persistence/import,
group comparison UI는 구현하지 않는다. 상세 제약은 ARCHITECTURE의 future profiles 절을 따른다.

## Bulk Timetable Input — current implementation, 2026-09-10

Supersedes the A/B “future” statuses above; C and other future features are unchanged.

| Feature | Current status | Boundary |
| --- | --- | --- |
| Clipboard table parser | IMPLEMENTED / AUTOMATED VERIFIED | Pure quoted TSV, rectangular validation, exact content |
| School Timetable Import (A) | IMPLEMENTED / USER NATIVE SMOKE PASSED | Unique 35-column signature, structural pairs, explicit candidate/mapping confirmation |
| Canonical Template Import (B) | IMPLEMENTED / USER NATIVE SMOKE PASSED | Strict 8×11, exact headers and ordered periods |
| Template copy | IMPLEMENTED / USER NATIVE SMOKE PASSED | Explicit clipboard command, no .xlsx file |
| Common Preview / Apply | IMPLEMENTED / AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED | Current single active week, all 35 values before notification, no persistence |
| Small Rectangular Paste (C) | FUTURE / NOT IMPLEMENTED | Ctrl+V currently enters School mode only |
| Multi-profile/date/semester/persistence | FUTURE / NOT IMPLEMENTED | No models, schema or additional target UI added |

See [Bulk verification](TIMETABLE-BULK-INPUT.md) and [ADR 0007](adr/0007-bulk-timetable-input.md).

## Clock/Status presentation — 2026-09-10

CurrentDateText는 구현된 foundation이며 아래 future customization과 구분한다.
현재 한 줄 standard layout은 native candidate이며 v1 final design contract가 아니다.
기존 Phase 0.8 Header smoke는 날짜 추가 전 결과다. 이후 날짜 가독성/resize 사용자 승인은 PERIOD-SCHEDULE-EDITING에 별도 기록했다. 새 날짜 배치의 자동 증거는
[Architecture의 검증 기록](ARCHITECTURE.md#current-date-and-future-clockstatus-presentation--2026-09-10)을 따른다.

| Feature | Status | Direction / boundary |
| --- | --- | --- |
| Clock/Status presentation presets | PLANNED | Standard/Large Digital/Compact/Minimal; 정확한 명칭/배치 미확정 |
| Clock size customization | PLANNED | Time/Date/Status별 크기, 굵기, 정렬, 간격; preset 기본값 + 사용자 override |
| 12/24-hour / seconds / AM-PM options | PLANNED | HH:mm:ss, HH:mm, h:mm:ss 또는 h:mm + 오전/오후; 추가 clock read 금지 |
| Date/weekday format options | PLANNED | 날짜/요일/상태 표시 선택, weekday 포함 날짜 형식; 현재 기본 yyyy년 MM월 dd일 |
| Digital typography option | PLANNED | System/digital/seven-segment 후보; 향후 font license 확인, 현재 dependency 없음 |
| Settings persistence | PLANNED | 미래 Clock / Status Display 설정 저장; 현재 object/schema/UI 미구현 |

[ADR 0008](adr/0008-clock-status-presentation.md): normal tick의 geometry 불변과
preset/setting 변경의 의도적 re-layout을 구분한다. Core 시간 의미와 Desktop 표시 책임을 유지한다.

### Font sources and mixed typography — future requirement

| Feature | Status | Direction / boundary |
| --- | --- | --- |
| Per-element font customization | PLANNED | Title/Time/AmPm/Date/Weekday/Status 각각 독립 source/family; mixed typography 허용, Header 단일 font 강제 금지 |
| Bundled fonts | PLANNED | 앱 resource/private font; 도입 전 license/배포 조건 확인, 예시 family 채택 미확정 |
| System font selection | PLANNED | Windows 설치 font 및 사용자 설치 family 선택 |
| Online font catalog/download/cache | PLANNED | 사용자 요청, approved HTTPS provider → download → validate/app-local cache → local FontFamily; startup/tick의 CDN 의존 금지 |
| Missing-font fallback | PLANNED | Missing system/cache 또는 offline/download 실패 시 bundled/default; crash/blank text/앱 사용 불가 방지, 향후 fallback 상태 표시 가능 |
| Custom preset portability | PLANNED | Stable source/family/provider identity; absolute path/font binary 의존 금지, 다른 PC에서 resolve/cache/fallback |
| Local Font File | OPTIONAL FUTURE — NOT IMPLEMENTED | 사용자 선택 TTF/OTF를 앱 전용 font로 등록하는 후보 |

Font identity schema는 후속 설계하며 machine-specific cache 경로는 runtime detail이다.
Desktop TTF/OTF/OpenType asset 우선 방향과 WPF/.NET 10 형식 spike를 구분한다.
WOFF/WOFF2 직접 지원을 가정하지 않는다. Provider/Family/License/Version-source metadata와
license 검증이 필요하며 불명확한 font의 자동 다운로드/재배포는 허용하지 않는다.
세부 요구와 현재 독립 TextBlock coupling audit는 ARCHITECTURE의 font sources 절을 따른다.
이번 milestone에는 font UI/picker/browser/downloader/cache/font·preset persistence를 구현하지 않는다.

## Period Schedule Editing Foundation — current implementation

| Feature | Status | Boundary / evidence |
| --- | --- | --- |
| Base Period Schedule Editing | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED | Context menu → seven-row HH:mm Draft; invalid retains dialog/state; Apply-and-close; chronological 1..7 enforced without reorder |
| Runtime Schedule Replacement | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED | One immutable accepted schedule swap, one immediate shared refresh; one clock + one schedule snapshot per cycle |
| Current Date Header | IMPLEMENTED — AUTOMATED / OBJECT VERIFIED + USER READABILITY/RESIZE SMOKE PASSED | Existing independent CurrentDateText/time/status retained and date regression tests maintained |
| Date-specific Period Override | PLANNED | Independent future effective schedule; base unchanged |
| Period Schedule Persistence | PLANNED | Current edits last only until exit; restart restores defaults |
| Clock/Font customization | PLANNED | Existing per-element/preset/font source/cache/fallback requirements preserved, no implementation added |

[ADR 0009](adr/0009-editable-base-period-schedule.md) and
[Period Schedule Editing verification](PERIOD-SCHEDULE-EDITING.md) are the current authority.

## Optional lunch Break label — approved future requirement

| Feature | Status | Rule / boundary |
| --- | --- | --- |
| 4→5교시 점심시간 표시 옵션 | PLANNED — default OFF | ON + Break + effective Period4.End <= time < Period5.Start이면 점심시간 · 5교시까지 기존 Countdown; touching에는 interval 없음 |
| Effective Day presentation integration | PLANNED — next Effective Day / Date Override design | Header/Countdown/Highlight와 같은 clock/effective schedule snapshot; 변경된 base/날짜 override의 4/5교시 identity 사용, 재조회/시간 hard-code 금지 |
| Lunch display Settings checkbox | PLANNED | Desktop 표시 옵션만; 현재 UI/schema/persistence 미구현 |

[ADR 0010](adr/0010-optional-lunch-break-presentation.md): Core의 5상태를 유지하고 Lunch
kind는 추가하지 않는다. 다른 gap/상태와 countdown/highlight 의미는 그대로다.
현재 런타임에는 적용하지 않았으며 모든 Break가 기존 기본 문구를 표시한다.

## Effective Day / Date Overrides — current milestone, 2026-09-10

These statuses supersede earlier future/exclusion rows for the same features.

| Feature | Status | Boundary |
| --- | --- | --- |
| Effective Day Resolution | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | One clock snapshot and one date resolution; coherent status/countdown/date/time/grid/highlight |
| Date Timetable Override | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | DateOnly weekday complete 7-cell immutable snapshot; remaining 28 cells use Base |
| Date Period Schedule Override | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | Independent complete chronological 1–7; gap/touching allowed; base unchanged |
| Date Override runtime/editor | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | Private runtime map; fixed date Draft; atomic Apply/remove; Cancel/X/Escape; weekends rejected |
| Cell editing provenance | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | Displayed Base/date source; fixed open target with explicit label; schedule-only stays Base |
| Optional Lunch Presentation | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | Default OFF menu, immediate refresh, captured effective 4→5 interval, Core remains Break |
| School/Canonical import | IMPLEMENTED — existing native evidence retained; current coexistence checked within documented native scope | Base-only target explicitly labeled; overrides preserved |
| Date-specific import target | PLANNED | No importer target expansion |
| Persistence | PLANNED | Overrides and lunch option reset on restart |
| Teacher Profiles/Groups | PLANNED | Timetable values can move to profile ownership; schedules remain school-day concern |
| Semester Sets | PLANNED | No model/storage/UI added |
| Clock presets / per-element fonts / Bundled-System-Online fonts | PLANNED | Existing requirements unchanged |

[ADR 0011](adr/0011-effective-day-and-date-overrides.md), [verification](DATE-OVERRIDES.md).

Native acceptance scope and restart evidence are recorded in [Date Overrides](DATE-OVERRIDES.md); automated coverage is broader than the observed native scenarios.

## Local Persistence Foundation — 2026-09-11

Current status supersedes earlier persistence exclusions; user native review accepted on 2026-09-11. Evidence limits are recorded in PERSISTENCE.md.

| Feature | Status | Boundary |
| --- | --- | --- |
| Native profile persistence | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | schema 1, per-user profile.json, full validation and atomic save |
| Base timetable persistence | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | independent 35 SubjectText/ClassText pairs |
| Base period schedule persistence | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | complete chronological seven periods |
| Date override persistence | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | independent complete day/schedule components; one logical save |
| Lunch option persistence | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED | boolean only; interval remains derived |
| Load failure A policy | IMPLEMENTED — AUTOMATED / OBJECT VERIFIED | original untouched, temporary defaults, all commits blocked, visible notice |
| Full Backup/Restore | PLANNED | no automatic repair/restore or backup UI |
| Legacy Migration | PLANNED | no legacy reads or writes |
| Multi-profile / Groups | PLANNED | profile envelope may evolve through schema migration |
| Semester Sets | PLANNED | no model/UI/import added |
| Single-instance activation UX | PLANNED | persistence already excludes a second cooperating writer |

[ADR 0012](adr/0012-native-local-profile-persistence.md), [verification](PERSISTENCE.md).

## Future Week Navigation / Date Header — 2026-09-11

사용자 추가 future UI requirement. 아래 항목은 **PLANNED — NOT IMPLEMENTED**이며,
현재 Persistence milestone의 구현/저장 범위를 확대하지 않는다. 기존 CurrentDateText는
실제 오늘 날짜 표시이고, 아래 column별 Date Header와 별개다.

| Feature | Status | Requirement / boundary |
| --- | --- | --- |
| Week Navigation | PLANNED | 시작 시 실제 현재 주 Monday–Friday; 왼쪽/오른쪽 화살표로 정확히 −7/+7일 이동; weekday 5 columns 유지 |
| Date Header | PLANNED | 각 요일 위에 대응하는 실제 DateOnly 표시; compact 후보 M/d, zero-padding 필수 아님; customization은 future Display Settings |
| Effective timetable per column | PLANNED | 표시 주의 각 날짜를 독립 resolve; 그 날짜의 complete 7-cell override 우선, 없으면 해당 Base weekday |
| Browsing / actual status separation | PLANNED | 다른 주를 보더라도 CurrentDateText/CurrentTimeText/Status/Countdown은 실제 clock 및 오늘 effective schedule 유지 |
| Date-aware Current Highlight | PLANNED | 실제 오늘이 표시 주에 있을 때만 오늘의 current period 강조; 다른 주의 동일 weekday/period 강조 금지 |
| Today header indication | PLANNED — presentation direction | 표시 주에 오늘이 있으면 date/weekday header에서 구분하는 방향; 정확한 색상/style은 future design |
| Editing while browsing | PLANNED | displayed date의 명시적 Base/date provenance에 따라 F2/double-click; 열린 target 고정; 문자열로 source 추측 금지 |
| School / Canonical Bulk Import target | EXISTING DECISION RETAINED | 항상 Base WeeklyTimetable 전용; browsing 중인 날짜로 자동 변경하지 않음 |
| Viewed-week persistence | OUT OF CURRENT SCOPE | 현재 profile.json 저장 대상에 추가하지 않음; future View도 기본 재시작 시 현재 주로 복귀 |
| Today button / return-to-current-week action / date click / week calendar | FUTURE CANDIDATES — NOT DECIDED | 이번 요구에서 확정하거나 구현하지 않음 |
| Remember last viewed week | FUTURE CANDIDATE — NOT DECIDED | 향후 명시적 사용자 설정으로 검토 가능; 현재 저장 schema/UI에는 없음 |

상세 의미와 future 검증 기준은
[Architecture — Future Week Navigation / Date Header](ARCHITECTURE.md#future-week-navigation--date-header--2026-09-11)를 따른다.
이 문서 추가는 현재 Persistence의 automated/native 완료 상태나 commit/push 승인 상태를 바꾸지 않는다.

## Week Navigation implementation — 2026-09-11

These states supersede the preceding future rows for this milestone.

| Feature | Status | Boundary |
| --- | --- | --- |
| Week Navigation | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | Monday start, exact ±7 days, no tick-driven navigation |
| Date Headers | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | M/d and 월–금, two lines, five aligned equal columns |
| Per-date Effective Timetable Columns | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | exact DateOnly complete-day fallback and typed provenance |
| Today Indicator | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | actual date, background only |
| Date-aware Current Highlight | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | zero cells outside actual today's displayed week |
| Browsing / actual status separation | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | Header/countdown/lunch use actual clock and schedule |
| Editing while browsing | IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED | captured Base/date target; Base-only bulk import |
| Last Viewed Week Persistence | NOT IMPLEMENTED | schema 1 unchanged; restart current week |
| Date Click/Calendar Picker | PLANNED / DEFERRED | no date action or calendar added |
| Today button | PLANNED / DEFERRED | no return-to-current-week command added |

[ADR 0013](adr/0013-viewed-week-and-date-header.md), [verification](WEEK-NAVIGATION.md).

User native UX approval: 2026-09-11. Arrow/header visual polish remains future styling/display
work and is explicitly not a milestone blocker. Evidence: [Week Navigation](WEEK-NAVIGATION.md).

## Styling / Display Presets Foundation — current 2026-09-11

This section supersedes historical PLANNED entries for the implemented subset.
[ADR 0014](adr/0014-display-presets-and-schema-v2.md), [Display Settings](DISPLAY-SETTINGS.md).

| Feature | Status | Scope |
| --- | --- | --- |
| Display Presets | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Four editable initial configurations and independent layout |
| Per-element typography | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Time/Date/Weekday/Status family/size/weight/style |
| System font selection | IMPLEMENTED — NATIVE REVIEW CONFIRMED | WPF installed families, logical names, safe fallback |
| Display Settings P2 | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Live Preview, Apply, OK, Cancel/X and current-preset Reset |
| Time/date format and visibility | IMPLEMENTED — NATIVE REVIEW CONFIRMED | 12/24h, AM/PM, seconds, date/weekday/status |
| Profile schema 2 / v1 compatibility | IMPLEMENTED — NATIVE REVIEW CONFIRMED | No rewrite on v1 load, full next-save upgrade |
| Week/header visual polish | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Chevron styles, date/weekday hierarchy, soft Today background |
| Bundled font catalog | PLANNED | No bundled assets |
| Online font catalog/download/cache | PLANNED | No network/cache implementation |
| Named custom presets | PLANNED | No multi-preset manager |
| Color/theme editor | PLANNED | No full theme/Fluent redesign |

## User-defined display presets — current 2026-09-11

This supersedes prior named-preset PLANNED and current-writer-v2 entries.

| Feature | Status | Scope |
| --- | --- | --- |
| User-defined display presets | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Stable ID, Save As, selection and Reset |
| Rename/update/delete | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Selected rename/update; inactive-only confirmed delete |
| Preset transaction persistence | IMPLEMENTED — NATIVE REVIEW CONFIRMED | One display/library save and last-Apply rollback |
| Schema v3 / v1-v2 compatibility | IMPLEMENTED — NATIVE REVIEW CONFIRMED | Strict full load, no startup rewrite, next-save upgrade |
| Preset import/export | PLANNED | No sharing UI or format |
| Bundled fonts | PLANNED | No font assets |
| Online font catalog | PLANNED | No network/download/cache |

[ADR 0015](adr/0015-user-display-presets-and-schema-v3.md), [evidence](DISPLAY-SETTINGS.md).

## Bundled + online fonts — current 2026-09-11

| Feature | Current status | Boundary |
| --- | --- | --- |
| Bundled catalog | IMPLEMENTED — NATIVE UX APPROVED | Pretendard, DSEG7 Modern/Classic, licensed WPF resources |
| Online catalog | IMPLEMENTED — NATIVE UX APPROVED | Orbitron, IBM Plex Mono, explicit pinned HTTPS download |
| Private local resolution / fallback | IMPLEMENTED — VERIFIED — NATIVE UX APPROVED | Exact hash, isolated cache, missing identity preserved |
| Mixed fonts and user presets | IMPLEMENTED — VERIFIED — NATIVE UX APPROVED | Time/Date/Weekday/Status, schema v4; strict v1/v2/v3 readers |
| Save As selector P2 | FIXED — VERIFIED — NATIVE UX APPROVED | Stable-ID matching, collection refresh/rename/Apply/Cancel/reopen |
| Local font import / full cache UI / preset import-export / themes | PLANNED | Outside current scope |

This current section supersedes earlier no-assets/no-network/PLANNED entries for the implemented font scope.
License provenance, final validation and native approval: [Font Catalog](FONT-CATALOG.md).

## Display preset import/export — current 2026-09-14

| Feature | Current status | Boundary |
| --- | --- | --- |
| Single user preset export | IMPLEMENTED — AUTOMATED VERIFIED / LIMITED USER NATIVE UX APPROVED | `.stwpreset`, deterministic UTF-8 JSON, Draft-library source |
| Strict import and preview | IMPLEMENTED — AUTOMATED VERIFIED / LIMITED USER NATIVE UX APPROVED | 64 KiB, full validation, no mutation before decision |
| ID/name collision handling | IMPLEMENTED — AUTOMATED VERIFIED / LIMITED USER NATIVE UX APPROVED | Update/copy/cancel; unique editable copy name; no silent overwrite |
| Portable font references | IMPLEMENTED — AUTOMATED VERIFIED / LIMITED USER NATIVE UX APPROVED | Bundled/System/Online identity only; fallback status; no auto-download |
| Settings transaction | IMPLEMENTED — AUTOMATED VERIFIED / LIMITED USER NATIVE UX APPROVED | Draft only until Apply/OK; Cancel rollback; no auto-activation |
| Whole preset library/profile backup | PLANNED | Outside this one-preset sharing format |

[Contract and format](PRESET-IMPORT-EXPORT.md), [ADR 0017](adr/0017-display-preset-files.md).
