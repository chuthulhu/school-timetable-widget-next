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
| Countdown presentation formatter | IMPLEMENTED — FOUNDATION / Phase 0.6 | A8, A7; Current Status Header Presentation Text | Desktop의 한국어 formatter 및 immutable 두 텍스트 결과, countdown 조합 검증, presentation contract tests. 다국어 infrastructure는 현재 범위 밖 |
| Current Status Header | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A4, A7, A8, A9, I15–I16; Current Status Header | 별도 UserControl의 두 텍스트 binding, 고정 높이 및 App activation 구현; 사용자 host Windows에서 표시/live update/기본 폭 및 가로 resize 안정성 확인 |
| Header ViewModel foundation | IMPLEMENTED — FOUNDATION / Phase 0.7 | A8, A9, I15–I16 | 두 read-only 문자열, 계산 결과 Apply, 같은 문자열 PropertyChanged 억제; clock/timer/layout 책임 없음 |
| Live refresh loop foundation | IMPLEMENTED — FOUNDATION / Phase 0.7 | A9, A5, I16 | Desktop DispatcherTimer 약 1초, Start 즉시 refresh, cycle당 snapshot 1회, Start/Stop/Dispose, missed tick replay 없음 |
| Actual Header XAML/rendering | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A4, A8, I15 | XAML/binding/object tests 통과; 고정 높이/fixed time column/Tabular/NoWrap 후보. 기본 폭의 clipping/겹침 없음과 가로 resize 안정성 사용자 확인; font glyph 지원/DPI/최소 폭은 별도 검증 |
| Actual app activation/wiring | IMPLEMENTED — USER NATIVE SMOKE PASSED / Phase 0.8 | A9, A5, I16 | App OnStartup에서 동일 clock/default schedule 주입, Start 후 Show, OnExit Dispose. 사용자 live refresh 확인 및 X 종료 후 process 소멸 확인; 계측된 cadence/Dispose 실행 증거는 아님 |
| Tray visibility refresh lifecycle | DEFERRED | A9, P6 | hide/show에 따른 loop Start/Stop 정책 미결정 |
| Suspend/resume integration | DEFERRED | A9, A5 | OS detection/event 연결 미구현; RefreshNow로 현재 상태 재평가 가능한 기반만 제공 |
| Application Clock / KRISS | PARTIAL — Phase 0.2 foundation | A5, I16–I20 | [0004](adr/0004-application-time-source.md); Core snapshot/interface, Desktop PC fallback, App 소유 경계, Tests fake/contract tests 구현. KRISS 동기화는 미구현; endpoint/client/보정/동시 전환 DEFERRED |
| Timetable Editing | PARTIAL — EDITING FOUNDATION COMPLETE / USER NATIVE SMOKE PASSED | M1–M2, A2, R16, I3, I6–I7; approved Editing Foundation | [0006](adr/0006-single-cell-in-memory-editing.md); 교과/반 별도 Draft, atomic one-cell in-memory Apply/Cancel; persistence 미구현 |
| Period Editing | PLANNED | M2–M3, P4, R6, R16 | [0004](adr/0004-application-time-source.md); time validation 상세 DEFERRED |
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
| Date-specific period schedule override | FUTURE REQUIREMENT — NOT IMPLEMENTED | DateOnly 날짜의 예외; 기본 schedule 불변; timetable override와 독립 |
| Effective day configuration | FUTURE REQUIREMENT — NOT IMPLEMENTED | timetable only / schedule only / both / neither; 같은 snapshot 날짜로 resolve한 구성을 Status/Highlight/Notification이 공유 |
| CurrentDateText | FUTURE REQUIREMENT — NOT IMPLEMENTED | yyyy년 MM월 dd일; 기존 CurrentTimeText(HH:mm:ss) 별도 유지; date/time/status 동일 IApplicationClock snapshot |

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
