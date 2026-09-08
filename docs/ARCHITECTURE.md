# Architecture

현재는 Phase 0 solution skeleton과 개발환경 bootstrap 단계다. 제품 기능은 아직 구현하지 않았다.
이 문서는 확정된 baseline과 설계 방향을 구분한다. 상세 계약은
[Product Contract](PRODUCT-CONTRACT.md), 진행 상태는 [Feature Map](FEATURE-MAP.md)을 따른다.

## Accepted baseline

- WPF + .NET 10 LTS + CommunityToolkit.Mvvm, MVVM: P1 / [ADR 0002](adr/0002-windows-desktop-stack.md).
- 공통 Application Clock: A5, I16–I20 / [ADR 0004](adr/0004-application-time-source.md).
- Settings의 Committed → Draft → Live Preview와 성공 Apply baseline: P2 / [ADR 0003](adr/0003-settings-transaction.md).
- Golden Reference는 behavior/evidence source: [ADR 0001](adr/0001-golden-reference-policy.md). Python 구조는 재사용하지 않는다.

## Architecture direction

- Feature-oriented 책임 경계로 기능별 변경을 작고 명확하게 유지한다.
- Windows adapter를 명시적 platform boundary에 격리한다.
- Persistence boundary에서 validation과 committed revision의 성공/실패를 다룬다.
- UI preview/runtime, committed state, persistence를 구분한다.
- 거대한 MainWindowViewModel/AppState 및 Shared/Utils dumping ground를 방지한다.

## Phase 0 project structure

사용자가 승인한 B안이며 [ADR 0005](adr/0005-phase-zero-project-structure.md)에 기록한다.
기존 `SchoolTimetableWidget.sln` 형식으로 다음 세 project만 관리한다.

```text
src/
  SchoolTimetableWidget.Desktop/  (net10.0-windows, WPF)
  SchoolTimetableWidget.Core/     (net10.0)
tests/
  SchoolTimetableWidget.Tests/    (net10.0)
scripts/
  check-dev-env.ps1
  bootstrap-dev.ps1

Desktop → Core
Tests   → Core
```

| Project | 책임 / 현재 범위 |
| --- | --- |
| Desktop | View/ViewModel, Windows integration, infrastructure adapter. 현재 template App/MainWindow만 존재하며 CommunityToolkit.Mvvm은 이 project에만 참조한다. |
| Core | 순수 계산, 상태 전이, product contract logic, 시간 abstraction, persistence/migration contract의 소유 경계. 현재 빈 class library이며 WPF/Toolkit/Desktop 의존성이 없다. |
| Tests | Core 중심 테스트의 진입점. xUnit v3 runner만 구성했으며 아직 제품 테스트는 없다. |

Core → Desktop 의존은 금지한다. Feature별 assembly를 추가하지 않고 project 내부 폴더/namespace로
책임을 나눈다. Feature/Platform/Infrastructure 상세 폴더는 실제 첫 코드가 필요할 때 생성한다.
거대한 AppState/MainWindowViewModel이나 Shared/Utils를 만들지 않는다.
공통 Application Clock, committed/Draft/Preview/persistence 분리는 기존 Accepted 계약을 유지한다.
구체적인 type/interface와 composition 구현은 다음 기능 단계에서 결정한다.

SDK는 `global.json`의 10.0.400 / `latestFeature` / stable-only 정책으로 선택한다.
설정·패키지 버전·검증 방법은 [Development Setup](DEVELOPMENT.md)을 따른다.

## Deferred decisions

| 항목 | 상태 |
| --- | --- |
| 세부 feature folder/namespace 및 type 배치 | DEFERRED — 실제 기능 코드 추가 시 |
| DI container와 composition 방식 | DEFERRED |
| Persistence technology/schema, concurrency 및 crash recovery | DEFERRED |
| Installer/updater technology와 배포 상세 | DEFERRED |
| Windows App SDK 사용 범위 | DEFERRED |

Skeleton build/test runner 검증은 제품 기능 구현 완료 또는 native UI 검증을 의미하지 않는다.
Clock endpoint/client/보정 구현 등 feature별 유보 사항은 Product Contract와 해당 ADR을 따른다.
