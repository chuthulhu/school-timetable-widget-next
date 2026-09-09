# Feature Map

새 제품의 간단한 tracking 문서다. [Product Contract v0.1](PRODUCT-CONTRACT.md)이 authoritative 계약이며,
아래 상태는 구현 진행 상태다. `PLANNED`는 승인된 방향의 구현 전 상태이고,
`DEFERRED`는 제공 범위나 상세 설계 결정을 남긴 상태다. 구현 또는 검증 완료를 뜻하지 않는다.
PLANNED 기능에도 계약에 명시된 DEFERRED 상세는 그대로 남아 있다.
`PARTIAL`은 명시된 기반만 구현했으며 전체 기능 완료가 아닌 상태다.

| Feature | 상태 | Product Contract ID / section | 관련 ADR / 남은 상세 |
| --- | --- | --- | --- |
| Repository / Skeleton | IMPLEMENTED — Phase 0 skeleton + dev bootstrap | Architecture / Development Setup | [0005](adr/0005-phase-zero-project-structure.md); 3 projects, CLI runner 구성. 제품 기능/native 검증 완료를 뜻하지 않음 |
| Timetable | PLANNED | A2, M1, I6–I7, R1–R3 | [0001](adr/0001-golden-reference-policy.md); 독립 35셀, plain text 보존 |
| Period current-resolution foundation | IMPLEMENTED — FOUNDATION / Phase 0.3 | M3, P4, R4, R6–R7, I16 | Core immutable 정의·기본 profile·snapshot 기반 int? 계산, 평일 [start,end), 입력 중복/겹침 거부, contract tests. 전체 editor validation은 DEFERRED |
| Current Status Core foundation | IMPLEMENTED — FOUNDATION / Phase 0.4 | A6, A5, P4; Current Status State Model | 5상태 immutable 사실 결과, snapshot 기반 계산, 공통 schedule 검증. Break와 다음 교시/transition 구현; 전체 UI 완료 아님 |
| Highlight integration | PLANNED | M4, R4–R5, I9, I16 | [0004](adr/0004-application-time-source.md); UI 연결·갱신 상세 DEFERRED |
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
| Timetable Editing | PLANNED | M1–M2, A2, R16, I3, I6–I7 | [0001](adr/0001-golden-reference-policy.md); 무손실 편집과 Save 실패 경계 |
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
