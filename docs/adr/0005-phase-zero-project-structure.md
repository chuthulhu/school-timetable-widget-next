# ADR 0005 — Phase 0 project structure

Status: **Accepted**

Date: 2026-09-08

Decision status: **APPROVED — 사용자의 Phase 0 B안 명시적 채택**.
[ADR 0002](0002-windows-desktop-stack.md)에서 유보했던 project 분할과 의존 방향을 확정한다.
제품 사용자 동작이나 P1–P10/A1–A5의 계약은 변경하지 않는다.

## Decision

`SchoolTimetableWidget.sln`에 세 project만 둔다.

| Project | Framework | 책임 |
| --- | --- | --- |
| SchoolTimetableWidget.Desktop | net10.0-windows / WPF | View/ViewModel, Windows integration, infrastructure adapter |
| SchoolTimetableWidget.Core | net10.0 | 순수 계산, 상태 전이, product contract logic, 시간 abstraction, persistence/migration contract |
| SchoolTimetableWidget.Tests | net10.0 | 초기 Core 중심 테스트 |

Project reference는 Desktop → Core, Tests → Core다. Core → Desktop은 금지한다.
CommunityToolkit.Mvvm은 Desktop에만 둔다. Core는 WPF 및 Toolkit에 독립적이다.
Feature-oriented 구성은 project 내부 폴더/namespace로 표현하고 feature별 assembly는 추가하지 않는다.
상세 폴더는 실제 코드와 함께 도입하며 Shared/Utils dumping ground를 만들지 않는다.

## Consequences

Phase 0는 template App/MainWindow, 빈 Core, test runner, 개발 bootstrap만 만든다.
시간표/Settings UI, clock/KRISS, persistence, installer/updater 구현은 포함하지 않는다.
공통 Application Clock과 transaction 계약은 ADR 0003/0004를 그대로 따른다.

**DEFERRED:** DI container/composition, persistence technology/format, installer/updater,
세부 feature folder, interface/type 배치와 Windows App SDK 사용 범위.
Legacy는 evidence source이며 구현 코드를 복사하지 않는다.

## Phase 0.6 test reference follow-up — 2026-09-09

사용자가 요청한 Desktop presentation formatter 검증에서 기존 Tests 재사용(A안)을 채택했다.
Tests → Desktop 참조를 추가하고 Tests target을 net10.0-windows로 변경했다.
Tests → Core와 Desktop → Core는 유지하며 Core → Desktop은 여전히 금지한다.
Core의 net10.0, WPF/Toolkit 독립성 및 세 project 제한을 유지한다.
Tests 자체에 UseWPF나 Toolkit 직접 package를 추가하지 않는다. Desktop의 WPF runtime dependency는
전이되므로 테스트 실행 환경에 Windows Desktop runtime이 필요하다.
WPF Application/Window를 만들지 않는 순수 formatter tests를 기존 runner에서 실제 실행해 검증했다.
결과는 [Architecture Phase 0.6](../ARCHITECTURE.md#phase-06-desktop-current-status-presentation-formatter-foundation)을 따른다.
