# Architecture

현재는 Phase 0.7 Header ViewModel + live refresh loop foundation까지 구현했다. Desktop loop가 한 snapshot으로 Core 상태/countdown과 한국어 formatter를 조립하고 표시 전용 ViewModel을 갱신한다. 실제 Header XAML/rendering, App activation/wiring과 Highlight/UI 연결은 아직 구현하지 않았다. 다국어 infrastructure는 현재 범위 밖이다.
이 문서는 확정된 baseline과 설계 방향을 구분한다. 상세 계약은
[Product Contract](PRODUCT-CONTRACT.md), 진행 상태는 [Feature Map](FEATURE-MAP.md)을 따른다.

## Accepted baseline

- WPF + .NET 10 LTS + CommunityToolkit.Mvvm, MVVM: P1 / [ADR 0002](adr/0002-windows-desktop-stack.md).
- Current Status 5상태 사실 계약: A6 / Product Contract의 Current Status State Model.
- Countdown 표시 의미: A7 / Product Contract의 Countdown Display Semantics.
- 한국어 Header 표시 문구: A8 / Product Contract의 Current Status Header Presentation Text.
- Header refresh/lifecycle: A9 / Product Contract의 Current Status Header Refresh Lifecycle.
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
  SchoolTimetableWidget.Tests/    (net10.0-windows; Phase 0.6)
scripts/
  check-dev-env.ps1
  bootstrap-dev.ps1

Desktop → Core
Tests   → Core
Tests   → Desktop  (Phase 0.6 presentation contract tests)
```

| Project | 책임 / 현재 범위 |
| --- | --- |
| Desktop | View/ViewModel, Windows integration, infrastructure adapter. 현재 template MainWindow, App composition root, PC fallback clock adapter와 Features/CurrentStatus의 한국어 formatter, 표시 전용 ViewModel, DispatcherTimer refresh loop foundation이 있다. Loop의 실제 App activation은 아직 없다. CommunityToolkit.Mvvm은 이 project에만 직접 참조한다. |
| Core | 순수 계산, 상태 전이, product contract logic, 시간 abstraction, persistence/migration contract의 소유 경계. 현재 Time/의 clock interface, immutable snapshot, source enum과 Features/Periods/의 교시 정의·기본 profile·current-period 계산, Features/CurrentStatus/의 5상태 계산·countdown 의미 정규화가 있으며 WPF/Toolkit/Desktop 의존성이 없다. |
| Tests | 기존 Core tests와 Desktop presentation/lifecycle tests의 진입점. xUnit v3로 Application Clock, current-period, Current Status, countdown, Header formatter 및 ViewModel/refresh loop contract tests를 실행한다. Fake clock과 dispatcher object test helper는 Tests 내부에만 둔다. |

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
  이 clock 테스트가 실제 KRISS 동기화나 concurrent source switching을 구현·검증했다는 뜻은 아니다. Current-period 계산 검증은 아래 Phase 0.3에서 구분한다.

Clock은 시간 사실만 제공한다. 교시 계산은 별도 Periods feature가 담당한다. Phase 0.2 당시 UI formatting,
countdown rounding, notification, persistence, 네트워크 및 system clock mutation은 구현하지 않았다.
이 foundation의 테스트용 수치를 제품 정책으로 확대하지 않는다.

## Phase 0.3 Current-period calculation foundation

- Core `Features/Periods/PeriodDefinition`은 sealed class의 get-only `PeriodNumber`, `Start`, `End`다.
  번호는 1–7이고 날짜 없는 시각은 `TimeOnly`를 사용한다. 생성 시 `Start < End`를 검증한다.
  날짜·UTC offset은 `ApplicationTimeSnapshot`의 책임이며 교시 정의에 섞지 않는다.
- `DefaultPeriodSchedule.Periods`는 M3의 기본 7교시를 한 곳에 정의한 읽기 전용 collection이다.
  요소도 immutable이다. 기본 profile일 뿐이며 계산기는 호출자가 전달한 정의를 사용한다.
- `CurrentPeriodResolver.Resolve(ApplicationTimeSnapshot snapshot, IEnumerable<PeriodDefinition> definitions)`는
  `int?`를 반환하는 순수 계산이다. `null`은 current-period 없음이며 UI 문자열이나 이유 분류를 담지 않는다.
  Clock을 주입·조회하지 않고 하나의 snapshot에서 파생한 `Date.DayOfWeek`와 `TimeOfDay`만 평가한다.
  월–금에 `Start <= time && time < End`이면 번호를 반환하며, 주말·gap·수업 전후에는 null이다.
  Source/revision/offset으로 판정 분기를 하거나 UTC/KST로 재변환하지 않는다. TimeOnly tick 정밀도를 유지한다.
- 계산 API의 valid schedule 전제는 null entry 없음, 유일한 번호, 겹치지 않는 구간이다.
  입력을 한 번 열거해 복사하고 전체를 검증한 뒤 판정한다. 중복·겹침은 `ArgumentException`으로
  드러내며 주말이나 현재 시각이 겹침 밖인 경우에도 거부한다. 순서에 따라 첫 항목을 선택하지 않는다.
  `[start,end)`상 끝과 시작이 맞닿는 두 구간은 겹침이 아니다.
- 이는 R6의 모호한 계산 방지를 위한 API 경계다. Editor의 허용 관계·오류 UX·정렬·gap 정책이나
  저장 가능한 profile의 완전성 검증을 확정하지 않는다. 계산 입력의 순서는 무관하고 누락 교시를
  자동 보충하지 않는다. 빈 입력은 null이다. 이 동작이 빈/부분 profile의 저장 승인을 뜻하지 않는다.
  향후 editor/import에서는 별도의 전체 validation이 필요하며 관련 DEFERRED 결정은 유지한다.
- `Tests/Periods/CurrentPeriodContractTests`는 승인된 기본값, 7구간의 시작 직전/start/종료 직전/end(1 tick 정밀도),
  사용자 지정 sub-second 구간, 모든 평일·주말, 긴 gap, source/offset 독립성, local 날짜,
  캡처 뒤 clock 날짜/source 전환, 맞닿음·겹침·중복·잘못된 입력을 검증한다.
  기본 snapshot은 직접 생성하고, clock 전환 증거에만 기존 Tests fake를 사용한다.
- Phase 0.3 당시 다음 교시, 쉬는시간 분류/countdown, Status Header/ViewModel, notification, KRISS/NTP,
  persistence, editor, 날짜별 예외는 미구현이다. Desktop composition/UI를 연결하지 않았다.
### Phase 0.3 verification — 2026-09-09

- `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build --logger "console;verbosity=normal"`
  모두 exit 0. Build warning 0 / error 0. 기존 clock 13 + 신규 period 45 = 58 passed, failed/skipped 0.
- Core MSBuild 평가: net10.0, UseWPF 없음, ProjectReference/PackageReference 없음,
  FrameworkReference는 Microsoft.NETCore.App만 존재. Source에서도 WPF/Toolkit/Desktop 의존 없음.
- bin/obj 제외 production source의 DateTime.Now / DateTimeOffset.Now / DateTime.UtcNow /
  DateTimeOffset.UtcNow 검색은 기존 Desktop PcFallbackApplicationClock의 DateTimeOffset.Now 1건뿐이다.
  Core/Periods 직접 시간 읽기 및 GetSnapshot 호출은 0건이다.
- 자체 검토에서 end exclusive, 주말 제외, source 독립성, snapshot 재사용, 입력 순서와 무관한 겹침 거부,
  계산/표시 분리 및 이번 범위의 과설계 여부를 확인했다. 제품 계약 변경이나 새 ADR은 없다.
- 검증은 Core contract tests와 build/source inspection이다. WPF 창 실행·활성화, native input,
  system clock 변경 또는 KRISS/NTP 통신을 수행하지 않았다.

## Phase 0.4 Current Status Core foundation

- A6 승인 사실과 5상태의 current/next/transition 계약을 구현 전에 Product Contract에 기록했다.
  기존 P4/A5/ADR 0004의 interval/clock 의미를 유지하며 별도 ADR을 추가하지 않았다.
- Core `Features/CurrentStatus/`가 `CurrentStatusKind`, `CurrentStatusResult`, `CurrentStatusResolver`를 소유한다.
  Kind는 BeforeFirstPeriod, InPeriod, Break, AfterLastPeriod, Weekend의 정확히 5개다.
  긴 gap도 Break이며 점심/공휴일/휴업일/특별일정 상태를 추론하지 않는다.
- Result는 sealed class, private constructor, get-only 필드와 5개 named factory로 구성한다.
  BeforeFirstPeriod/Break factory는 유효한 PeriodDefinition의 번호와 Start를 next/transition에,
  InPeriod factory는 번호와 End를 current/transition에 넣는다. null period는 거부하며
  AfterLastPeriod/Weekend는 current/next/transition이 모두 null이다. 임의 필드 조합 생성이나 변경을 허용하지 않는다.
- `CurrentStatusResolver.Resolve(ApplicationTimeSnapshot snapshot, IEnumerable<PeriodDefinition> definitions)`는
  `CurrentStatusResult`를 반환한다. clock을 주입하거나 읽지 않고 snapshot의 local Date/TimeOfDay만 사용한다.
  source/revision/offset 분기나 UTC/KST 재변환 없이 tick 정밀도를 유지한다.
- `Features/Periods/PeriodScheduleValidator`는 두 resolver만 공유하는 internal 계산 검증 helper다.
  Phase 0.3 검증을 추출하여 입력을 한 번 열거·복사하고 null entry/중복 번호/겹침을 거부한다.
  번호 1–7과 Start < End는 기존 immutable PeriodDefinition의 생성자가 보장한다.
  모든 검증은 주말 판정보다 먼저 실행한다. CurrentPeriod의 API, 빈 입력 → null 및 판정 동작은 유지한다.
- CurrentStatus만 빈 입력을 ArgumentException으로 거부하고 복사본을 Start 기준 Array.Sort로 정렬한다.
  caller collection은 변경하지 않는다. 유효한 부분 schedule도 제공된 정의 기준으로 계산한다.
  첫/다음/마지막은 번호나 입력 순서가 아닌 시간 순서다. 이 허용이 persisted profile의 유효성 승인은 아니다.
  Editor/import의 1–7 전체 존재 등 완전성 정책과 validation UX는 별도 단계다.
- 두 resolver는 schedule validation만 공유하고 별도 순수 계산을 한다. CurrentPeriod를 내부에서 다시 호출해
  입력 복사·검증을 반복하거나 API를 status용으로 확대하지 않는다. CurrentStatus는 정렬 후 한 번 순회하며
  시작 전이면 BeforeFirstPeriod/Break, 종료 전이면 InPeriod, 모두 지났으면 AfterLastPeriod다.
  [start,end) 의미상 정확한 End는 종료한 교시에서 벗어나며, 맞닿은 Start에서는 즉시 다음 InPeriod다.
- TransitionTime은 날짜 없는 예정 local school time(TimeOnly?)이다. BeforeFirstPeriod는 first.Start,
  InPeriod는 current.End, Break는 next.Start, AfterLastPeriod/Weekend는 null이다. Countdown duration이 아니다.
- Tests/CurrentStatus는 기본 예시와 7교시 모든 경계의 1 tick, 긴 gap, 평일/주말, 맞닿음,
  순서 뒤섞임과 caller 불변성, 번호와 시간 순서가 다른 부분 schedule, 단일 subsecond 교시,
  source/offset/revision 독립성, local/UTC 날짜 차이, 캡처 뒤 clock 전환, 두 resolver 일관성,
  빈/잘못된 schedule의 주말 이전 거부, 단일 열거와 Result invariant를 검증한다.
  기존 Clock/CurrentPeriod 테스트는 수정·삭제하지 않았다.
- Phase 0.4 당시 Core는 WPF/Toolkit/Desktop에 독립적이다. 사용자 문자열, countdown 계산/formatting, Header ViewModel/XAML,
  Highlight integration, notification, KRISS/NTP, persistence, editor는 추가하지 않았다.

### Phase 0.4 verification — 2026-09-09

- SDK 10.0.401에서 dotnet restore, dotnet build --no-restore,
  dotnet test --no-build --logger "console;verbosity=normal" 모두 exit 0.
  Build warning 0 / error 0. 기존 clock 13 + period 45 + 신규 Current Status 40 = 98 passed,
  failed/skipped 0. 기존 58개 테스트 파일은 변경하지 않았다.
- 최초 sandbox restore는 .dotnet/10.0.401.toolpath.sentinel 접근 제한으로 실패했다.
  승인된 실행 권한으로 restore를 재실행해 성공했다. 새 SDK 첫 실행 출력에는
  CLI 초기화와 ASP.NET Core HTTPS 개발 인증서 자동 설치가 포함됐다. 인증서 trust 명령은 실행하지 않았다.
- Core MSBuild 평가: net10.0, UseWPF 없음, ProjectReference/PackageReference 없음,
  FrameworkReference는 Microsoft.NETCore.App만 존재. Source에서도 WPF/Toolkit/Desktop 의존 없음.
- bin/obj 제외 production .cs의 DateTime.Now / DateTimeOffset.Now / DateTime.UtcNow /
  DateTimeOffset.UtcNow 검색은 기존 Desktop PcFallbackApplicationClock의 DateTimeOffset.Now 1건뿐이다.
  CurrentStatus 및 Periods feature의 직접 시간 읽기/clock 조회는 0건이다.
- 자체 검토: 정확히 5상태, 점심 추론 없음, end exclusive와 맞닿은 즉시 다음 InPeriod,
  상태별 transition/필드 invariant, 시간 정렬과 caller 불변성, source/revision 독립성과 snapshot 재사용,
  Core/표시 분리, empty/partial 계산과 persisted validity의 구분 및 Phase 0.3 동작 보존을 확인했다.
- 증거는 Core contract tests, build와 source inspection이다. WPF 창 실행·활성화, native input,
  clipboard 변경, system clock 변경 또는 KRISS/NTP 통신을 수행하지 않았다.
  Header/Highlight UI나 Desktop adapter의 실제 native 동작 검증으로 해석하지 않는다.

## Phase 0.5 Countdown Core semantics

- 구현 전에 A7 Countdown Display Semantics의 사용자 승인(2026-09-09)을 Product Contract에 기록했다.
  A4 현재 시각 HH:mm:ss와 별도로 countdown 초 생략, 전체 남은 분 floor, LessThanMinute,
  hours/minutes 정규화 및 0분 없는 exact transition 의미를 확정했다. 새로운 ADR은 필요하지 않으며
  ADR 0004의 기존 formatting 유보에 후속 A7 결정 링크만 추가했다.
- Core Features/CurrentStatus/에 CountdownDisplayValue와 CurrentStatusCountdownCalculator를 추가했다.
  기존 CurrentStatusResult/Resolver, Periods, Time 및 Desktop production 코드는 변경하지 않았다.
- CountdownDisplayValue는 sealed class, get-only LessThanMinute(bool), Hours(int), Minutes(int)만 제공한다.
  Public constructor/factory/setter가 없고 internal constructor는 양수 remainingTicks만 받아 정규화한다.
  LessThanMinute이면 Hours/Minutes = 0/0이다. 그 외 Hours >= 0, Minutes 0..59이며
  Hours == 0이면 Minutes >= 1이다. Exact TimeSpan/ticks를 결과에 보관하지 않는다.
- API는 CurrentStatusCountdownCalculator.Calculate(ApplicationTimeSnapshot snapshot, CurrentStatusResult status)
  → CountdownDisplayValue?다. BeforeFirstPeriod/InPeriod/Break는 status의 TransitionTime까지 계산하고,
  AfterLastPeriod/Weekend는 기존 결과 invariant(transition 없음)에 따라 null이다. 긴 gap도 Break다.
- Caller는 resolver와 calculator에 같은 snapshot을 전달한다. Calculator는 clock 조회, schedule 재조회,
  resolver 재호출, source/revision/offset 분기 및 UTC/KST 재변환을 하지 않는다.
  모든 provenance를 검사하는 API는 아니며 날짜/schedule/snapshot 일치 보장은 caller 책임이다.
- 같은 local school day의 transition.Ticks - snapshot.TimeOfDay.Ticks로 정확한 차이를 구한다.
  TimeOnly의 wrap-around subtraction을 사용하지 않는다. Start < End 계약을 유지하며 자정을 넘는 교시는 지원하지 않는다.
  차이가 0 이하이면 잘못된 argument 조합이므로 ArgumentException(nameof(status))으로 거부한다.
  이는 정상 transition UX가 아니라 stale/inconsistent input 방어다. Null arguments는 ArgumentNullException이다.
- 양수 ticks / TimeSpan.TicksPerMinute의 정수 나눗셈으로 floor한다. 0 wholeMinutes는 LessThanMinute,
  그 외 wholeMinutes / 60 및 % 60이 Hours/Minutes다. 1분은 (0,1,false), 59.9999999초는 (0,0,true),
  1시간 00분 59초는 (1,0,false), 1시간 1분은 (1,1,false), 2시간은 (2,0,false)다.
- 향후 Desktop presentation이 status kind/current/next와 의미 값을 조합한다. Hours > 0 및 Minutes == 0은
  시간만 표시하는 의미다. Core는 한국어/localized 문자열, 단위/접두 문구, formatter/localization framework를 제공하지 않는다.
  Notification duration/scheduling, KRISS/NTP, persistence 또는 다른 기능 책임으로 확장하지 않았다.
- Tests/CurrentStatus/CurrentStatusCountdownContractTests는 승인 예시, 1 tick/subsecond duration과 minute/hour 경계,
  모든 기본 start/end 전후 resolver→calculator, 맞닿은 교시, AfterLast/Weekend null, source/revision/offset 독립성,
  캡처 뒤 clock 변경, 세 countdown 상태의 stale exact/지난 transition 거부, null arguments와 public 생성/변경 차단을 검증한다.
  하루 안의 각 minute 경계 양쪽에서 의미 값의 범위와 실제 남은 시간과의 floor 오차도 검사한다.
  기존 98개 테스트를 담은 파일은 변경·삭제하지 않았다.

### Phase 0.5 verification — 2026-09-09

- SDK 10.0.401에서 dotnet restore, dotnet build --no-restore,
  dotnet test --no-build --logger "console;verbosity=normal" 모두 exit 0.
  Build warning 0 / error 0. 기존 98 + 신규 countdown 34 = 132 passed, failed/skipped 0.
- Core MSBuild 평가: net10.0, UseWPF 없음, ProjectReference/PackageReference 없음,
  FrameworkReference는 Microsoft.NETCore.App만 존재. WPF/Toolkit/Desktop/localization 의존을 추가하지 않았다.
- bin/obj 제외 production .cs의 DateTime.Now / DateTimeOffset.Now / DateTime.UtcNow /
  DateTimeOffset.UtcNow 검색은 PcFallbackApplicationClock의 DateTimeOffset.Now 1건뿐이다.
  새 countdown Core의 직접 시간 읽기/clock 조회는 0건이다.
- 자체 검토에서 positive sub-minute와 0분의 구분, floor, hour/remainder, exact transition의 새 상태,
  AfterLast/Weekend null, stale input 예외, snapshot 재사용 및 Core/표시 책임 분리를 확인했다.
- 검증 증거는 Core contract tests, build/MSBuild와 source inspection이다. WPF 창 실행·활성화, native input,
  clipboard 변경, system clock 변경 및 KRISS/NTP 통신을 수행하지 않았다.
  Countdown presentation formatting/localization, Status Header ViewModel/XAML, Highlight UI와 KRISS sync는 미구현이다.

## Phase 0.6 Desktop Current Status presentation formatter foundation

- 구현 전에 Product Contract A8에 한국어 단일 언어, 두 텍스트 분리, HH:mm:ss,
  5상태 문구와 countdown 변환의 사용자 승인(2026-09-09)을 기록했다.
- Desktop `Features/CurrentStatus/CurrentStatusHeaderFormatter`는 static 순수 formatter다.
  `Format(ApplicationTimeSnapshot snapshot, CurrentStatusResult status, CountdownDisplayValue? countdown)`
  → `CurrentStatusHeaderText`를 제공한다. Result는 sealed class, internal constructor와
  get-only string 속성 `CurrentTimeText`, `StatusText`만 가진다. WPF/Toolkit/UI type은 사용하지 않는다.
- 현재 시각은 `snapshot.LocalTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)`이며
  timezone/source/revision을 노출하거나 UTC/KST로 재변환하지 않는다. 교시/시간/분 숫자도
  FormattableString.Invariant로 Arabic digits를 유지한다. 모든 한국어 production 표시 문자열은 formatter에만 둔다.
- StatusText는 A8의 5문구와 ` · ` 구분자를 그대로 사용한다. BeforeFirstPeriod/Break는
  NextPeriodNumber, InPeriod는 CurrentPeriodNumber를 사용한다. Countdown은 LessThanMinute를
  먼저 처리하고 minutes-only, hours-only, hours + minutes로 변환한다. Floor/duration 재계산은 없다.
  Core가 보장하는 semantic value invariant를 신뢰하므로 정상 입력에서 0분을 만들지 않는다.
- BeforeFirstPeriod/InPeriod/Break의 null countdown과 AfterLastPeriod/Weekend의 non-null countdown은
  `ArgumentException(nameof(countdown))`으로 거부한다. Null snapshot/status는 ArgumentNullException이다.
  전체 날짜/schedule/provenance 검증은 하지 않으며 caller가 동일 snapshot으로 계산된 입력을 전달한다.
- Formatter는 clock/Resolver/Calculator/schedule을 조회하지 않는다. KRISS sync, notification,
  persistence, timer/polling, Header ViewModel/XAML, Highlight UI와 localization framework를 추가하지 않았다.
- 기존 Tests에 Desktop ProjectReference를 추가하고 target을 net10.0-windows로 변경했다.
  Tests의 UseWPF 설정이나 신규 test project는 필요하지 않았다. Desktop의 WPF runtime 참조가
  테스트 실행에 전이되므로 Windows Desktop runtime이 필요하지만 WPF Application/Window나 STA 입력은 필요하지 않다.
  Desktop의 CommunityToolkit.Mvvm 8.4.2 직접 dependency는 그대로다. Core는 net10.0 독립성을 유지한다.
  [ADR 0005 후속 기록](adr/0005-phase-zero-project-structure.md#phase-06-test-reference-follow-up--2026-09-09)에 이 구조 변경을 기록했다.
- 신규 presentation contract tests는 승인 예시, zero-padding/24시간제/소수초 생략, 1 tick 및 minute/hour
  표시 경계, custom schedule의 current/next 번호, 5상태 null 조합, source/revision/offset 독립성을 검증한다.
  ko-KR/en-US/ar-SA와 의도적으로 변경한 시간 구분자를 dedicated thread에만 적용하고 finally로 복원한다.
  DefaultThreadCurrentCulture 등 process 기본 culture는 바꾸지 않는다. 기존 132개 tests의 source는 변경하지 않았다.

### Phase 0.6 verification — 2026-09-09

- dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger "console;verbosity=normal" 실행.
  첫 restore는 NuGet 서비스 인덱스를 읽지 못해 NU1900 1건을 보고했고, 권한을 확장한 일반 restore도
  캐시된 경고를 보고했다. 같은 권한에서 dotnet restore --force --no-http-cache로 재조회해 exit 0,
  warning/error 0을 확인했다. NuGet 감사 비활성화 및 DNS/proxy/credential 설정 변경은 하지 않았다.
- Build exit 0, warning 0 / error 0. 기존 132 + 신규 presentation 37 = 169 passed, failed/skipped 0.
  기존 테스트 프로젝트의 Desktop 참조와 Windows target으로 실제 runner 실행에 성공했다.
- Core MSBuild 평가: net10.0, UseWPF 없음, ProjectReference/PackageReference 없음,
  FrameworkReference는 Microsoft.NETCore.App만 존재한다. bin/obj 제외 Core production source의
  한국어 표시 단어(쉬는시간/교시/오늘 수업/분/시간), WPF/Toolkit/Desktop 참조 검색은 0건이다.
- bin/obj 제외 production .cs의 DateTime.Now / DateTimeOffset.Now / DateTime.UtcNow /
  DateTimeOffset.UtcNow는 기존 PcFallbackApplicationClock의 DateTimeOffset.Now 1건뿐이다.
  Formatter의 직접 시간 읽기, GetSnapshot, Resolver/Calculator 호출 및 WPF/Toolkit 의존 검색은 0건이다.
  Core, 기존 tests와 XAML/code-behind의 diff는 없다.
- 검증 증거는 contract tests, build/MSBuild와 source inspection이다. WPF 창 실행·활성화,
  native input, clipboard 변경, system clock 변경 또는 KRISS/NTP 통신을 수행하지 않았다.
  이 결과는 Header UI, font/layout, live update 또는 native 동작 검증이 아니다.

## Phase 0.7 Header ViewModel and live refresh loop foundation

- 구현 전에 Product Contract A9에 약 1초 cadence, 즉시 Start refresh, cycle당 snapshot 1회,
  같은 snapshot pipeline, 표시/lifecycle 책임 분리와 catch-up 금지의 사용자 승인을 기록했다.
- `CurrentStatusHeaderViewModel : ObservableObject`는 get-only CurrentTimeText/StatusText만 공개한다.
  초기값은 string.Empty이며 `Apply(CurrentStatusHeaderText)`가 backing field를 SetProperty로 변경한다.
  같은 문자열은 알림을 발생시키지 않는다. 시각만 바뀌면 CurrentTimeText만, 교시 경계에서 두 문자열이
  바뀌면 각 property를 알린다. 외부 public setter, clock/schedule/계산기/timer, UI styling/layout 상태는 없다.
- `CurrentStatusHeaderRefreshLoop` 생성자는 IApplicationClock, IEnumerable<PeriodDefinition>, ViewModel을
  받는다. Schedule은 생성 시 배열로 복사해 loop 수명 동안 유지하고 각 refresh에서 기존 Core resolver가
  검증한다. DefaultPeriodSchedule hardcode나 provider/repository/service abstraction은 추가하지 않았다.
  Editable period persistence의 schedule source 교체는 별도 설계다.
- Loop는 생성한 dispatcher에 속한 실제 DispatcherTimer(Background priority)를 소유한다.
  Interval은 TimeSpan.FromSeconds(1)이며 real-time deadline 보장이 아니다. Public API는 Start, Stop,
  RefreshNow, Dispose 및 get-only IsRunning이다. 생성과 모든 호출은 소유 UI dispatcher thread에서
  수행해야 하며 VerifyAccess로 다른 thread의 사용을 거부한다. ViewModel.Apply도 같은 UI thread에서 호출한다.
- RefreshNow는 정확히 한 번 `_clock.GetSnapshot()`을 호출한다. 그 snapshot을 Resolver → CountdownCalculator
  → Formatter에 그대로 전달하고 전체 계산 성공 후 ViewModel.Apply를 호출한다. Stopped 상태에서도 사용 가능하며
  timer를 켜지 않는다. Source/revision/offset 판단, 직접 PC 시간 읽기 및 별도 countdown 계산은 없다.
- Start는 즉시 RefreshNow 후 timer를 시작한다. 이미 running이면 no-op이며 Stop은 반복 호출 가능하다.
  Stop 후 Start는 현재 snapshot으로 즉시 새 refresh를 한다. Initial refresh 실패는 호출자에게 전파하고
  timer를 시작하지 않는다. Initial notification 중 중복 Start도 no-op이며 Stop/Dispose가 발생하면
  외부 Start가 돌아와 timer를 다시 켜지 않는다. IsRunning은 즉시 refresh 중인 activation도 포함한다.
- Dispose는 Stop 후 Tick handler를 해제한다. 반복 Dispose/Stop은 안전하며 disposed Start/RefreshNow는
  ObjectDisposedException으로 거부한다. Tick handler는 실행 중일 때 RefreshNow 한 번만 호출한다.
  Missed tick 수 계산이나 재생은 없으며 09:49:58 → 09:50:03 점프도 현재 Break 한 번만 반영한다.
- App/MainWindow 및 code-behind는 변경하지 않았다. Phase 0.8에서 기존 App 소유 clock, 사용할 schedule과
  ViewModel을 loop 생성자에 전달하고 실제 Header UI lifecycle에서 Start/Dispose를 연결할 경계다.
  표시 소비자가 없는 현재 앱에서 불필요한 1Hz loop를 실행하지 않는다. 실제 live Header 표시는 아직 없다.
- Header XAML/rendering/layout, Highlight, KRISS/NTP, notification, persistence 및 period editor는 미구현이다.
  Tray visibility lifecycle과 suspend/resume detection/integration은 DEFERRED다. 향후 show/resume에서
  RefreshNow를 사용할 수 있다는 것이 해당 OS integration 구현을 뜻하지 않는다.

### Phase 0.7 verification — 2026-09-09

- dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger "console;verbosity=normal" 모두 exit 0.
  Warning/error 0. 기존 169 + 신규 22 = 191 passed, failed/skipped 0.
- 신규 CurrentStatusHeaderRefreshContractTests는 표시 속성/알림, 단일 snapshot 경계, 지연 후 재평가,
  Start/Stop/Dispose, 초기 refresh 실패, 주입 schedule, source/revision/offset 독립성 및 thread 경계를 검증한다.
  기존 169개 테스트 소스, Core, formatter, project/package 및 XAML/code-behind는 변경하지 않았다.
- Loop 테스트는 dedicated STA thread에 실제 DispatcherTimer를 생성하고 finally에 dispatcher를 종료한다.
  Reflection은 이 저장소 loop의 private timer 설정/IsEnabled 확인과 OnTick handler 직접 호출에만 사용한다.
  WPF 내부 event 저장 구조에는 의존하지 않는다. Tick 구독/Dispose 해제는 source inspection으로 확인한다.
  실제 1초 sleep, Dispatcher message pump, WPF Application/Window 생성·실행은 하지 않는다.
- 증거는 contract/object/event tests, build/MSBuild 및 source inspection이다. 실제 timer cadence/delivery,
  Header rendering/font/layout, App activation, native keyboard/IME/focus 동작 검증이 아니다.
  창 활성화, native input, clipboard 변경, system clock 변경 및 KRISS/NTP 통신을 수행하지 않았다.
