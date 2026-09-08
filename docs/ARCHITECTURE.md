# Architecture

현재는 Phase 0.2 Application Clock foundation까지 구현했다. 학교 상태 계산과 UI 기능은 아직 구현하지 않았다.
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
| Desktop | View/ViewModel, Windows integration, infrastructure adapter. 현재 template MainWindow, App composition root와 PC fallback clock adapter가 있다. CommunityToolkit.Mvvm은 이 project에만 참조한다. |
| Core | 순수 계산, 상태 전이, product contract logic, 시간 abstraction, persistence/migration contract의 소유 경계. 현재 Time/의 clock interface, immutable snapshot, source enum이 있으며 WPF/Toolkit/Desktop 의존성이 없다. |
| Tests | Core 중심 테스트의 진입점. xUnit v3와 Tests 내부 fake clock으로 첫 Application Clock contract tests를 실행한다. |

Core → Desktop 의존은 금지한다. Feature별 assembly를 추가하지 않고 project 내부 폴더/namespace로
책임을 나눈다. Feature/Platform/Infrastructure 상세 폴더는 실제 첫 코드가 필요할 때 생성한다.
거대한 AppState/MainWindowViewModel이나 Shared/Utils를 만들지 않는다.
공통 Application Clock, committed/Draft/Preview/persistence 분리는 기존 Accepted 계약을 유지한다.
시간 type/interface와 최소 composition은 아래 Phase 0.2 경계를 따른다. 다른 기능의 상세 구조는 해당 단계에서 결정한다.

SDK는 `global.json`의 10.0.400 / `latestFeature` / stable-only 정책으로 선택한다.
설정·패키지 버전·검증 방법은 [Development Setup](DEVELOPMENT.md)을 따른다.

## Deferred decisions

| 항목 | 상태 |
| --- | --- |
| 세부 feature folder/namespace 및 type 배치 | DEFERRED — 실제 기능 코드 추가 시 |
| DI container | 도입하지 않음; App에서 직접 소유하고 향후 소비자 constructor에 주입 |
| Persistence technology/schema, concurrency 및 crash recovery | DEFERRED |
| Installer/updater technology와 배포 상세 | DEFERRED |
| Windows App SDK 사용 범위 | DEFERRED |

Skeleton build/test runner 검증은 제품 기능 구현 완료 또는 native UI 검증을 의미하지 않는다.
Clock endpoint/client/보정 구현 등 feature별 유보 사항은 Product Contract와 해당 ADR을 따른다.

## Phase 0.2 Application Clock foundation

- Core `Time/IApplicationClock.GetSnapshot()`은 소비자가 현재 시간을 얻는 공통 경계다.
  한 번의 관련 상태 계산에서 한 번 읽고, 같은 `ApplicationTimeSnapshot`을 하위 계산에 전달한다.
- Snapshot은 sealed class와 get-only 값으로 구성된다. 저장 값은 `LocalTime` (`DateTimeOffset`),
  `Source` (`ApplicationTimeSource`), `Revision` (`long`)뿐이다.
  `Date` (`DateOnly`)와 `TimeOfDay` (`TimeOnly`)는 LocalTime에서 파생하므로 따로 변경되거나 clock을 다시 읽지 않는다.
- LocalTime은 instant와 해당 source의 local UTC offset을 함께 보존한다. UTC instant가 필요하면
  `LocalTime.ToUniversalTime()`으로 구한다. 모호한 DateTimeKind나 별도 날짜/시각 저장 필드를 두지 않는다.
- `PcLocalFallback`은 PC local offset을 그대로 보존한다. PC timezone이 KST가 아니어도
  KST로 강제 변환하거나 KST라고 표시하지 않는다. 이는 A5의 PC local fallback 의미를 따른다.
  `SynchronizedStandardTime`은 KST(+09:00) 표현이며 생성 시 offset을 검증한다.
  enum은 KRISS vendor/endpoint와 결합하지 않는다. DateTimeOffset은 timezone ID나 미래 DST 규칙까지 담지는 않는다.
- Revision은 해당 clock 수명 안에서 authoritative reference/source가 바뀐 세대를 식별한다.
  persistence revision, 매초 counter, 전역 ID가 아니다. 현재 adapter는 reference 전환 없이 0을 유지한다.
  PC 시계 진행/수정 자체를 새 sync reference로 간주하지 않는다. 미래 sync 전환의 원자적 구현은 유보한다.
- Desktop `Infrastructure/Time/PcFallbackApplicationClock`만 `DateTimeOffset.Now`를 한 번 읽는다.
  이 명시적 OS read 경계만 직접 system time API 예외다. Core 및 feature/UI는
  `DateTime.Now`, `DateTimeOffset.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow`를 직접 읽지 않는다.
- `App.ApplicationClock`은 App 생성 시 fallback 구현을 한 번 생성·소유하는 composition 전용 속성이다.
  실제 소비자가 생기면 App에서 같은 instance를 constructor로 전달한다. 소비자에서
  `Application.Current`를 통해 clock을 찾는 service locator 패턴은 사용하지 않는다.
  아직 소비자가 없어 실제 ViewModel 주입은 없으며 StartupUri와 template MainWindow는 유지한다.
- Fake clock은 `tests/SchoolTimetableWidget.Tests/Time/`에만 있다. Contract tests는
  offset/날짜/시각/source/revision 및 자정·source 전환 중 snapshot 전달 방식을 검증한다.
  실제 KRISS 동기화, concurrent source switching 또는 학교 상태 계산을 구현·검증했다는 뜻은 아니다.

Clock은 시간 사실만 제공한다. UI formatting, countdown rounding, 교시 계산, notification,
persistence, 네트워크 및 system clock mutation은 구현하지 않았다. 향후 교시 계산은 승인된
`[start,end)`를 사용하며 이 foundation의 테스트용 수치를 제품 정책으로 확대하지 않는다.
