# Architecture

현재 Phase 0.8 Header XAML + App startup/shutdown wiring은 IMPLEMENTED — USER NATIVE SMOKE PASSED다. 기존 A4–A9를 실제 View에 연결하고 사용자 host Windows에서 표시/live update/가로 resize와 종료를 확인했다. 검증 범위는 아래 native smoke 기록을 따른다. Weekly Timetable Core/read-only View도 구현했고 사용자 native smoke를 통과했다. Current Highlight integration도 구현했고 아래 범위의 사용자 native smoke를 통과했다. 교과/반 한 셀 Editing Foundation도 in-memory 범위의 자동 검증과 사용자 native smoke를 통과했다. 다국어 infrastructure는 도입하지 않는다.
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
| Desktop | View/ViewModel, Windows integration, infrastructure adapter. 현재 MainWindow의 Header/Timetable feature View, App startup/shutdown composition, PC fallback clock adapter와 Features/CurrentStatus의 한국어 formatter, 표시 전용 ViewModel, Header와 current slot을 동일 snapshot으로 갱신하는 DispatcherTimer refresh loop가 있다. 실제 Header/live app refresh는 구현했고 사용자 host Windows의 제한된 native smoke를 통과했다. CommunityToolkit.Mvvm은 이 project에만 직접 참조한다. |
| Core | 순수 계산, 상태 전이, product contract logic, 시간 abstraction, persistence/migration contract의 소유 경계. 현재 Time/의 clock interface, immutable snapshot, source enum과 Features/Periods/의 교시 정의·기본 profile·current-period 계산, Features/CurrentStatus/의 5상태 계산·countdown 의미 정규화, Features/Timetable/의 immutable 35셀 모델이 있으며 WPF/Toolkit/Desktop 의존성이 없다. |
| Tests | 기존 Core tests와 Desktop presentation/lifecycle tests의 진입점. xUnit v3로 Application Clock, current-period, Current Status, countdown, Header formatter, ViewModel/refresh loop 및 Header XAML/binding 및 Timetable model/presentation/XAML/layout contract tests를 실행한다. Fake clock과 dispatcher object test helper는 Tests 내부에만 둔다. |

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
| Native v1 persistence/schema/write exclusion/load failure | RESOLVED — ADR 0012; broader backup/restore recovery remains DEFERRED |
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

## Phase 0.8 scope before implementation — 2026-09-09

- 기존 A4–A9를 Header UserControl, binding 및 App startup/normal shutdown에 연결한다.
  새 사용자-visible 제품 계약을 추가하지 않고 Core와 Phase 0.7 계산/loop 계약을 유지한다.
- Header 높이 56 DIP, 가로 padding 16 DIP, font size 16 DIP, time column 112 DIP와
  두 텍스트 사이 여백 16 DIP는 native 검토용 초기 구현 후보다. 승인된 영구 수치가 아니며
  Product Contract에 고정하지 않는다. MainWindow의 기존 800 × 450 후보 크기는 유지한다.
- Time은 fixed column + tabular numeral, status는 남은 폭 + NoWrap/CharacterEllipsis를 사용한다.
  실제 font glyph와 폭/높이 안정성은 native 사용자 확인 전까지 검증 완료로 표시하지 않는다.
  최소 창 크기와 overflow의 최종 UX는 P3 DEFERRED를 유지한다.
- Weekday header/35셀, Highlight, final styling, Settings, KRISS/NTP, tray, suspend/resume,
  single-instance, autostart, notification, persistence, installer/updater는 구현하지 않는다.
- 자동 검증 뒤 실제 창을 준비하고 사용자 확인을 기다린다. 사용자 native 확인 전 staging,
  commit/push를 하지 않는다. 창 visibility는 process 실행 여부와 별도로 확인한다.

## Phase 0.8 Header View and App wiring — user native smoke passed

- `CurrentStatusHeaderView : UserControl`의 XAML이 별도 CurrentTimeText/StatusText TextBlock을
  OneWay로 binding한다. Code-behind는 InitializeComponent만 수행한다. 기존 ViewModel,
  formatter, RefreshLoop와 Core 코드는 변경하지 않았다.
- View가 고정 높이를 소유하며 time은 fixed column과 Typography.NumeralAlignment=Tabular,
  status는 남은 star column과 NoWrap/CharacterEllipsis를 사용한다. WPF 기본 font family와
  system separator brush를 사용한다. 위 DIP 수치는 조정 가능한 native 후보이며 최종 styling이 아니다.
  설치된 WPF 10.0.12 reference XML에서 NumeralAlignment attached property와 Tabular enum을 확인하고
  실제 XAML build/object test로 설정을 확인했다. Font의 실제 glyph 지원과 렌더링은 별도 native 확인 대상이다.
- MainWindow는 Header를 Row 0 (Auto)에 배치하고 Row 1 (*)은 비워 둔다. 기존 800 × 450과
  SizeToContent.Manual을 유지하며 새 minimum/resize 정책은 추가하지 않았다.
  생성자에서 받은 기존 Header ViewModel을 feature View의 DataContext에 연결할 뿐 계산을 소유하지 않는다.
- App의 StartupUri를 제거하고 OnStartup에서 App 소유 clock, DefaultPeriodSchedule.Periods와
  새 Header ViewModel로 loop를 생성한다. MainWindow 생성 → loop.Start → MainWindow.Show 순서로
  첫 표시 전에 두 문자열을 준비한다. Clock은 기존 App 소유 instance 하나이며 service locator가 없다.
- App이 loop lifetime을 소유한다. OnExit에서 Dispose하고 초기 startup 실패도 Dispose 후 예외를 전파한다.
  정상 종료는 기존 WPF 기본 shutdown 방식이며 tray/close-to-hide 정책은 도입하지 않는다.
  Startup/normal shutdown 이외 OS lifecycle integration은 DEFERRED다.

### Phase 0.8 automated verification — 2026-09-09

- dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger "console;verbosity=normal" 실행.
  최종 결과 모두 exit 0, warning/error 0. 기존 191 + 신규 4 = 195 passed, failed/skipped 0.
- 최초 신규 binding tests 2개는 queued WPF binding 작업을 처리하기 전 빈 target을 관찰하여 실패했다.
  Test helper에서 dedicated STA dispatcher의 queued 작업을 ApplicationIdle까지 처리하도록 보완했다.
  UpdateTarget 강제 호출이나 실제 1초 Sleep을 사용하지 않는다. 수정 후 전체 195개를 다시 실행해 통과했다.
- 신규 tests는 실제 compiled XAML 생성, 초기 binding, RefreshNow를 통한 경계 전후 binding 갱신,
  두 TextBlock/고정 높이/고정 시각 열/NoWrap/Tabular 설정, MainWindow의 feature View 주입과 빈 아래 영역을 검증한다.
  숫자 후보를 고정 assertion으로 만들거나 font pixel/geometry를 검증한 것으로 주장하지 않는다.
- Tests는 dedicated STA에서 UserControl과 표시하지 않는 MainWindow 객체를 생성하고 정리한다.
  Binding queue를 처리하는 object test이며 timer cadence/delivery 또는 native window smoke 증거가 아니다.
  WPF Application 생성/실행, Window.Show, native input, clipboard/system clock 변경을 테스트에 넣지 않았다.
- Core, 기존 191 tests, Phase 0.7 loop/ViewModel/formatter와 package/project 설정은 변경하지 않았다.
  Core 독립성과 직접 system clock 읽기 예외는 유지한다.
- 자동 검증 완료 시점에는 IMPLEMENTED — PENDING NATIVE REVIEW로 기록했고 staging/commit/push를 하지 않았다.
  이후 사용자 native 확인 결과는 아래에 별도로 기록한다.

### Phase 0.8 user native smoke — 2026-09-09

- Codex에서 dotnet run으로 실행한 PID 74136은 응답 및 window handle이 있었지만 사용자가 창이
  보이지 않는다고 보고했다. Desktop isolation 가능성이 있으나 원인을 확정하지 않았고 앱 실패로
  판정하지 않았다. 해당 실행의 경로를 확인한 뒤 소유 process만 중지했다. 이 중지는 정상 종료 증거가 아니다.
- 사용자가 일반 host PowerShell에서 같은 Desktop project를 실행했다. 제공한 screenshot에
  School Timetable Widget 창, 위쪽 독립 Header, 왼쪽 16:09:22, 오른쪽
  '7교시 · 종료까지 40분' 및 아래 빈 영역이 보였다. 기본 schedule의 16:00–16:50와
  countdown floor 의미에 맞으며 제공된 기본 폭 화면에서 clipping/겹침은 관찰되지 않았다.
- 사용자가 약 5초 관찰 요청에 '정상'으로 답해 약 1초 시각 갱신, status 시작 위치 및 Header 높이의
  눈에 띄는 흔들림 없음을 확인했다. 가로 폭을 조금 줄였다 늘리는 확인에도 '정상'으로 답해
  두 줄 증가, 글자 겹침, window geometry의 갑작스러운 변화가 없음을 확인했다.
- 사용자 실행의 executable 경로와 PID 78816을 먼저 읽었다. 사용자가 X로 창을 닫고 '닫음'으로
  응답한 뒤 해당 PID의 종료와 동일 이름 앱 process가 남지 않음을 read-only process 조회로 확인했다.
  OnExit → Dispose는 source 확인, 실제 창 닫기와 process 종료는 사용자 조작 및 process 관찰 증거다.
  Dispose 내부 실행을 계측하거나 사용자 PowerShell의 exit code를 수집한 것은 아니다.
- 이 결과로 Header XAML/wiring과 live app refresh는 IMPLEMENTED — USER NATIVE SMOKE PASSED다.
  UI 수정 없이 사용자 검토를 통과했지만 DIP 후보값을 영구 Product Contract 수치로 승격하지 않는다.
  Timer의 정확한 cadence 계측, font glyph의 OpenType 지원 입증, 실제 시각의 모든 상태 경계,
  극단적으로 좁은 폭, DPI/multi-monitor, tray/suspend/resume 및 최종 visual design 검증으로 확대하지 않는다.
- 사용자 native 확인 직후에는 검증 결과만 문서에 반영했고 staging/commit/push를 하지 않았다. 이후 milestone 종료 요청에 따라 최종 self-review와 자동 검증을 거쳐 commit/push한다.

## Weekly Timetable Read-Only View — 2026-09-10

Status: Core **IMPLEMENTED — FOUNDATION**; Weekly View / Weekday Header
**IMPLEMENTED — USER NATIVE SMOKE PASSED**. 기존 A2/M1/I6–I7 및 A4 Header 아래 배치를 구현한다.
Product Contract/Accepted ADR 변경은 없다. Highlight, editing, persistence는 PLANNED다.

### Fixed-reference visual evidence

사용자가 2026-09-10 허용한 최소 source 확인으로 Golden Reference
`84de32a555633120bd6363a609a19cbc0a15e8ea`의
[Widget.init_ui](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/src/gui/widget.py#L175)
grid 생성 및 label 설정을 read-only로 확인했다. 이는 source-confirmed behavior evidence이며
이번에 Legacy를 실행하거나 native 화면을 다시 확인한 증거가 아니다.

- 전체 grid는 8행 × 6열이다. 첫 행은 빈 좌상단 + 월/화/수/목/금이다.
- 첫 열의 2–8행은 숫자 문자열 `1`–`7`이다. `1교시` 같은 접미사는 없다.
- 헤더 13개 = 빈 corner 1 + weekday 5 + period 7이다.
- 본문은 나머지 7행 × 5열의 독립 35셀이다. 헤더는 본문 데이터가 아니다.
- Header/body 모두 수평·수직 가운데 정렬이고 body는 word wrapping이 켜져 있다.
  Header에는 wrapping을 켜지 않는다.
- Legacy AutoText는 재현하지 않는다. 승인된 R3/I6대로 WPF plain text를 표시한다.
  Python 구조, Qt grid API, 정확한 간격/색/크기는 구현 template으로 가져오지 않았다.

요청의 5 columns는 본문 기준으로 유지한다. Legacy에서 확인된 왼쪽 교시 header column을
별도로 표현하므로 새 row-label UX나 제품 결정을 추가한 것이 아니다.

### Ownership and implementation

- Core `Features/Timetable/`에는 SchoolDay, TimetableCell, WeeklyTimetable만 둔다.
  SchoolDay는 Monday–Friday의 의미 값이며 한국어 표시나 legacy JSON key를 소유하지 않는다.
- TimetableCell은 sealed/get-only Day, PeriodNumber, Content다. 잘못된 day/period와 null content를
  생성 시 거부한다. 빈 문자열은 유효하며 공백/Unicode/newline/markup-looking 문자열을 변환하지 않는다.
- WeeklyTimetable은 입력을 한 번 열거해 새 배열에 슬롯별로 배치하고 전체 35슬롯의 존재와 중복을 검증한다.
  누락/null/중복을 암묵적으로 보충하지 않는다. read-only collection과 day/period indexer를 제공하며
  순서는 period 1–7, 각 period 안에서 Monday–Friday다. 각 슬롯과 문자열은 immutable이다.
  Empty()는 35개의 서로 다른 빈 슬롯을 명시적으로 생성하는 factory이며 저장 schema 결정이 아니다.
- Desktop `Features/Timetable/`의 WeeklyTimetableViewModel은 weekday/period 표시 목록과
  35개의 TimetableCellViewModel을 읽기 전용으로 소유한다. 셀 VM은 Content만 제공하고,
  같은 문자열이라도 별도 객체다. clock/current/upcoming/style/editor 상태가 없다.
- WeeklyTimetableView는 2×2 배치 안에 corner, weekday ItemsControl, period ItemsControl,
  body ItemsControl을 둔다. 각 ItemsPanel은 각각 1×5, 7×1, 7×5 UniformGrid다.
  별도 교시 열과 본문 열/행이 정렬되고 본문은 동일 폭·높이를 갖는다.
  XAML TextBlock.Text OneWay binding, Wrap, Center, TextTrimming=None을 사용한다.
  Code-behind는 InitializeComponent만 수행한다.
- MainWindow는 Row 0 Header / Row 1 Timetable feature View와 주입받은 두 VM의 DataContext만 연결한다.
  교시 계산, clock, timetable indexing이나 수동 35셀 생성은 없다.
  App이 기존 clock/default period schedule/Header loop lifetime을 유지하고 timetable VM을 추가 주입한다.
  Header의 Core/formatter/ViewModel/refresh loop/XAML은 변경하지 않았다.
- WindowContentMinimum은 `Desktop/Infrastructure/Windows/`의 작은 WPF attached behavior다.
  Loaded와 가로 크기 변경 시 현재 client 폭에서 content를 높이 무제한으로 측정하여
  필요한 창 MinHeight를 갱신한다. content의 MinWidth와 실제 window/content 크기 차이를 합쳐
  창 chrome을 포함한 minimum을 적용하고 정상 star-row 측정으로 돌아간다.
  Header tick에는 연결하지 않는다. preferred geometry 저장/변경, OS 설정, timer는 없다.
  일반 가로 resize의 사용자 native 결과는 아래에 기록한다. Chrome/minimum의 극단 경계와 DPI는 별도 검증 범위다.
- 800×600 시작 창, 본문 최소 폭 500 DIP, 교시 열 48 DIP, 글자 16 DIP,
  padding 8/6 DIP, 셀 minimum 44 DIP는 조정 가능한 구현 후보다.
  고정된 Product Contract 수치나 최종 디자인으로 승격하지 않는다.
  전체 Settings Preview/minimum, preferred/applied geometry persistence, DPI/monitor 및
  화면보다 큰 content minimum의 overflow UX를 구현·검증했다고 주장하지 않는다.

### Runtime fixture policy

기본 실행은 App에서 명시적 빈 주간 데이터를 주입한다. 실제 신규 profile/default subject 정책이나
persistence를 확정하는 데이터가 아니다. `--timetable-preview`를 명시한 경우에만
`Development/TimetablePreviewData`를 사용하고 창 제목에 '개발용 시간표 미리보기'를 표시한다.
이 fixture는 저장되지 않고 모든 실행에서 deterministic하다.

대표 위치는 월1/월2 국어(별도 셀), 화1 두 줄 물리학/실험 A반!, 수1 긴 문장,
목1 literal `<b>과목</b>`, 금1 빈칸, 화2 앞뒤 공백, 수2 whitespace-only,
목2 Unicode, 금7 마지막 수업이다. Production Core에 dummy 기본값이나 DEBUG framework를 추가하지 않았다.

### Automated verification and self-audit

- `dotnet restore`, `dotnet build --no-restore`,
  `dotnet test --no-build --logger "console;verbosity=normal"` 실행 완료, 최종 exit 0.
  Build warning 0 / error 0. 기존 195 + 신규 33 = **228 passed**, failed/skipped 0.
- 신규 Core 21 cases: 모든 35슬롯 접근, invalid day/period, 누락/중복/null 거부,
  input 복사/단일 열거, read-only API, 빈 값·공백·Unicode·newline·literal markup·반복 값 보존.
- 신규 Desktop presentation 4 cases: 월화수목금/숫자 1–7 순서, body ordering,
  35개의 독립 VM, text/read-only/null 정책.
- 신규 WPF 8 cases: STA compiled XAML, 5+7+1 header 및 35 body, binding/literal Run,
  800/620/500 DIP에서 상대 geometry와 content fit, 긴 multiline의 minimum 증가,
  synthetic Loaded의 minimum 적용, 주입 clock 경계 갱신 중 body subtree 재측정/위치 변화 없음.
  Font pixel이나 candidate 수치를 영구 계약으로 고정하는 assertion은 없다.
- 기존 195개 중 MainWindow composition test 하나는 '빈 아래 영역'을 'Timetable View 주입'으로 갱신했다.
  Header에 대한 기존 assertion은 유지했고 나머지 기존 테스트와 time/current-status production 코드는 불변이다.
- 최초 실행은 226 passed / 2 failed였다. Test assembly에서 compiled XAML UserControl을 상속한
  계측 subclass는 resource assembly mismatch로 실패했고, 빈 text에 Run 하나를 기대한 assertion도 실패했다.
  Test-only Decorator 계측과 빈 inline collection 검사로 수정한 뒤 전체 228개를 재실행했다.
  Production XAML loading이나 문자열 표시의 실패로 기록하지 않는다.
- 검증은 표시하지 않는 WPF 객체의 Measure/Arrange/binding queue 및 synthetic event다.
  Application 실행/Window.Show, foreground input, clipboard 변경, system clock 변경은 없었다.
  Test-only Decorator 계측은 native rendering/cadence/OS resize 증거가 아니다.
- Core MSBuild: net10.0, UseWPF 없음, ProjectReference/PackageReference 없음,
  FrameworkReference는 Microsoft.NETCore.App만 존재한다.
  직접 system time read는 기존 PcFallbackApplicationClock의 DateTimeOffset.Now 1건뿐이다.
- 자체 감사: 35 독립 슬롯과 순서, header/body 분리, literal text, no trim/merge,
  MainWindow composition, Timetable/CurrentStatus 독립, fixture 분리 및 최소 styling을 확인했다.
  P1/P2 finding 없음. 신규 persistence/migration/backup/editing/highlight는 없다.
  숫자 후보·Legacy quirks를 새 계약으로 만들거나 Python implementation을 복제하지 않았다.

### Native checkpoint preparation — historical

사용자 승인 전 commit/push하지 않는다. 현재 도구 실행이 실제 사용자 로그인 desktop을 보장하지 못하므로
보이지 않는 sandbox 앱을 먼저 실행하지 않는다. 이는 앱 실행 실패 판정이 아니다.
사용자의 일반 host PowerShell에서 다음 명령으로 정상 창을 실행한다:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --no-build --project 'D:\Codex\school-timetable-widget-next\src\SchoolTimetableWidget.Desktop\SchoolTimetableWidget.Desktop.csproj' -- --timetable-preview
```

먼저 창이 실제로 보이는지 확인한다. 이후 다음을 순서대로 확인하고 기록한다:

1. 기존 Status Header의 현재 시각/status 및 약 1초 갱신.
2. 월화수목금, 왼쪽 1–7 숫자, 독립된 본문 7행×5열.
3. 대표 한글/두 줄/긴 text/literal 태그/빈칸/반복 과목 표시, 기본 창의 clipping/겹침 없음.
4. 가로 resize 시 열/행 정렬 및 text fit, Header 갱신 중 grid 흔들림 없음.
5. X로 정상 종료 및 해당 사용자 실행 process 소멸.

이 준비 시점에는 native 항목이 모두 PENDING이었다. 이후 실제 결과는 아래에 기록한다.
사용자 승인 후 결과 반영 → restore/build/test/diff check/self-review → 승인된 commit/push 순서로 계속한다.

### User native smoke — 2026-09-10

- 사용자가 일반 host PowerShell에서 개발 preview 실행 명령을 사용하고 실제 창의 screenshot을 제공했다.
  이번에는 Codex sandbox에서 먼저 앱을 실행하지 않았다.
- Screenshot에는 '개발용 시간표 미리보기' 제목, 09:33:36과 '1교시 · 종료까지 16분',
  월화수목금, 왼쪽 숫자 1–7 및 독립된 본문 7×5가 보인다. 기본 1교시 schedule과 countdown floor에 맞는다.
- 월1/월2의 별도 국어 셀, 화1의 물리학/실험 A반! 두 줄, 수1 긴 문장의 자동 wrap,
  목1의 태그를 포함한 literal `<b>과목</b>`, 빈 셀, Unicode와 금7 마지막 수업을 확인했다.
  제공된 화면에서 명백한 clipping/겹침은 관찰되지 않았다.
  공백 문자의 정확한 개수 보존은 screenshot만으로 입증하지 않으며 model/binding tests의 증거다.
- 약 5초간 매초 시각 갱신과 Header/grid 위치 흔들림 여부를 묻는 확인에 사용자가
  '정상적으로 작동'이라고 답했다. 이어 가로 폭을 조금 줄였다 늘리며 정렬/잘림/겹침을 확인하는
  요청에 '정상'이라고 답했다. 이 결과로 제한된 live update/가로 resize native smoke를 통과했다.
- 종료 전 해당 repo의 Debug executable 경로와 PID 30632를 read-only로 관찰했다.
  사용자가 제목 표시줄 X로 닫고 '닫음'이라고 답한 뒤 PID 30632가 없고 같은 이름의 앱 process가
  0개임을 확인했다. 강제 종료하지 않았다. OnExit/Dispose 연결은 source와 기존 lifecycle tests,
  실제 X 종료/process 소멸은 사용자 조작 및 process 조회 증거다. 사용자 shell exit code나
  Dispose 내부 실행, 정확한 timer cadence를 계측한 것은 아니다.
- Weekly Timetable read-only View와 Weekday Header를 IMPLEMENTED — USER NATIVE SMOKE PASSED로 갱신한다.
  최소 폭/높이의 모든 조합, DPI/multi-monitor, 최대 content, 화면보다 큰 minimum의 overflow,
  모든 실제 시간 경계 및 최종 styling을 검증한 것으로 확대하지 않는다.
- 사용자의 native 확인 결과와 최초 milestone 지시에 따라 별도 승인 요청 없이 최종
  restore/build/test, diff check, self-review 후 commit/push를 진행한다.

### Final verification after native acceptance — 2026-09-10

- Native 확인 후 restore/build/test를 다시 실행했다. 모두 exit 0,
  build warning/error 0, 전체 228 passed 및 failed/skipped 0이다.
- 최종 source/diff review에서 기존 Header 계산/refresh/lifecycle 및 Core 독립성을 재확인했다.
  Native 검토 뒤 production code 수정은 없으며 관찰 결과와 상태만 문서에 반영했다.
  P1/P2 finding 없음. Git diff check와 신규 파일 whitespace 검사도 통과했다.

## Current Timetable Cell Highlight Integration — 2026-09-10

Status: **IMPLEMENTED — USER NATIVE SMOKE PASSED (limited scope below)**. M4/R4–R5/I9/I16 및 A9를 구현하며
Product Contract/Accepted ADR 변경은 없다. 과거 milestone 절의 미구현 표기는 당시 기록이다.

### Refresh and identity ownership

- 기존 CurrentStatusHeaderRefreshLoop를 **CurrentStatusRefreshLoop**로 rename했다.
  같은 Features/CurrentStatus 안에서 두 presentation의 갱신을 조립한다.
  단일 DispatcherTimer, 약 1초 interval, Start/Stop/Dispose/thread 경계 및 missed-tick replay 없음은 유지한다.
  Generic event bus, global state, DI container, 별도 highlight timer/coordinator는 추가하지 않았다.
- 생성자에 WeeklyTimetableViewModel을 필수로 주입한다. App은 같은 timetable VM을
  MainWindow와 loop에 전달하며 MainWindow의 XAML/code-behind는 변경하지 않았다.
- RefreshNow는 snapshot을 정확히 한 번 읽고 같은 snapshot으로 status → countdown → header text와
  current slot을 계산한다. 계산 성공 후 같은 dispatcher 호출 안에서 Header와 timetable에 적용한다.
  Await, dispatcher yield, 재조회나 별도 clock은 없다. 동일 UI cycle이 끝나면 두 view가 같은 사실을 표시한다.
  임의 외부 PropertyChanged handler를 위한 여러 VM의 transaction API를 도입한 것은 아니다.
- Desktop Timetable의 CurrentTimetableSlot.From은 이미 계산한 status/snapshot을
  nullable tuple `(SchoolDay Day, int PeriodNumber)?`로 투영한다.
  InPeriod일 때만 local Date.DayOfWeek를 명시적 Monday–Friday switch로 변환한다.
  Saturday/Sunday와 나머지 네 status는 null이다. 날짜/status 재계산이나 clock 조회는 없다.
  Null snapshot/status는 거부한다. Caller는 같은 snapshot에서 계산된 status를 제공한다.
- Core의 기존 SchoolDay/PeriodNumber identity를 재사용하며 Core 파일/type/schema를 변경하지 않았다.
  한국어 표시문구나 과목 문자열을 identity로 사용하지 않는다.
- WeeklyTimetableViewModel의 SetCurrentCell(nullable tuple)은 null로 해제하며,
  생성 시 구성한 slot→cell VM dictionary로 대상 하나를 찾는다.
  Invalid day/period는 이전 상태를 바꾸기 전에 예외로 거부한다. Partial nullable day/period 조합은 API에 없다.
  같은 cell 재적용은 no-op이고 이동 시 old false → new true만 알린다.
  Old-cell PropertyChanged 중 다른 슬롯을 지정하는 경우에도 외부 선택을 덮어써 두 current를 남기지 않는다.
- TimetableCellViewModel은 기존 Content와 externally get-only IsCurrent만 보유한다.
  ObservableObject의 동일값 알림 억제를 사용하고 변경 메서드는 internal이다.
  Brush/color/style/border/clock/resolver/editor 상태는 없다.
  Empty/whitespace-only/반복 과목도 동일한 slot 규칙을 적용한다.

### Visual and layout invariance

BodySlotBorder style은 기존 SlotBorder를 그대로 상속하고 IsCurrent DataTrigger에서
**Background만** 변경한다. 현재 후보는 연한 노란색 `#FFF3CD`이며 theme/settings의 최종 값이 아니다.
BorderThickness/padding/font/weight/wrapping/MinHeight/template/order를 상태에 따라 바꾸지 않는다.
기존 35개 셀과 TextBlock을 그대로 유지하고 weekday/period header에는 trigger를 적용하지 않는다.

이는 Golden Reference에서 확인된 border 두께/HFW 변화 quirk를 복제하지 않고,
승인된 R5/I9의 layout 불변 의미를 따르는 WPF 구현이다. Legacy 소스/API/스타일 구조를 복사하지 않았다.
WindowContentMinimum, MainWindow dimensions 및 preferred-size/persistence 정책은 이번 diff에서 불변이다.
Highlight final theme/settings와 Upcoming은 계속 DEFERRED다.

### Deterministic native preview

정상 실행과 기존 `--timetable-preview`는 실제 PC fallback Application Clock을 유지한다.
명시적 **`--highlight-preview`**에서만 기존 representative timetable과 HighlightPreviewClock을 주입한다.
창 제목은 '강조 검증 · 모의 시각 (70초 순환)'으로 바뀌며 실제 현재 시각으로 오인하지 않도록 구분한다.
App이 선택한 clock 하나를 동일 production refresh pipeline에 전달한다.

이 개발 clock은 TimeProvider timestamp의 elapsed time을 이용해 아래 10초 단계들을 70초 주기로 반복한다.
Read 횟수로 시간을 증가시키거나 missed ticks를 재생하지 않는다. 테스트는 timestamp만 주입한다.
Windows system clock, 실제 default period schedule, 사용자 데이터 또는 KRISS/NTP를 변경하지 않는다.
Snapshot Source/Revision은 synthetic fixture metadata이며 실제 PC/KRISS 정확성 주장이 아니다.

| 실행 후 구간 | 시작 mock time / 관찰 대상 |
| --- | --- |
| 0–10초 | 월요일 09:10:00, 월1 국어 |
| 10–20초 | 월요일 09:49:55, 5초 뒤 exact End → Break/강조 해제 |
| 20–30초 | 월요일 09:59:55, 5초 뒤 exact Start → 월2 국어 |
| 30–40초 | 금요일 09:10:00, 금1 empty cell |
| 40–50초 | 수요일 10:10:00, 수2 whitespace-only cell |
| 50–60초 | 금요일 16:49:55, 금7 마지막 수업 → 5초 뒤 AfterLast/해제 |
| 60–70초 | 토요일 09:10:00, Weekend/강조 없음 |

단계 경계의 시간/날짜 jump는 의도한 개발 fixture다. 사용자 기능이나 영구 기본값이 아니다.
Preview가 주입하는 날짜/과목은 이미 확인한 fixture이며 저장되지 않는다.

### Automated verification and self-audit

- `dotnet restore`, `dotnet build --no-restore`,
  `dotnet test --no-build --logger "console;verbosity=normal"` 최종 exit 0.
  Build warning/error 0. **기존 228 + 신규 48 = 276 passed**, failed/skipped 0.
- Logic/VM 23 cases: 정확한 slot 선택, 모든 평일 mapping, 반복/empty/whitespace 내용,
  no-current 4상태와 양 주말, invalid identity/null 거부, no-op 알림, old/new 이동 및 observer 재진입.
- Refresh integration 9 cases: 09:49:59 → 09:50:00 → 10:00:00에서 snapshot당 1회 read와
  두 view 일치, 큰 날짜/time jump와 no replay, weekday/date stale 제거, 맞닿은 custom periods,
  local-date/source/revision 경계, 계산 실패 전 상태 유지와 새 필수 dependency.
- WPF object 4 cases: 800/620/500 폭에서 여러 슬롯의 on/move/off 전후 전체 셀 DesiredSize,
  RenderSize, 상대 위치, text/wrapping/font/border/padding 불변. Trigger 후 measure validity 유지 및
  강제 재측정 결과도 동일함을 확인했다. Empty cell background 변경과 header 불변도 확인했다.
  구체 color pixel 값을 고정한 assertion은 없다.
- Development clock 12 cases: 70초 단계, exact start/end, empty/whitespace/주말 및
  elapsed-time jump/반복 read 불변. 실제 system clock read 없이 fixture를 검증한다.
- 기존 테스트의 loop type/constructor call을 rename/필수 timetable 주입에 맞춰 갱신했다.
  Header와 기존 Timetable assertion은 유지했다. 기존 Header/grid geometry test도 실제 주입된
  timetable VM의 highlight 전환을 포함해 통과했다.
- 최초 신규 build의 xUnit assertion-style warning 4개는 권장 assertion overload로 수정했다.
  첫 전체 test run은 272 passed / 4 failed였다. WPF test helper가 ItemsControl의 외곽 Border까지
  셀로 세어 36개를 얻고 slot index도 한 칸 밀린 것이 원인이었다.
  DataContext가 cell VM이고 TextBlock을 직접 포함하는 body Border만 검사하도록 수정한 뒤
  전체 276개를 다시 실행해 통과했다. Production 셀 수나 layout 결함으로 기록하지 않는다.
- Source audit: Core/Product Contract/ADR/MainWindow/WindowContentMinimum 불변,
  refresh의 GetSnapshot 1곳, 별도 highlight clock read/timer 없음, 문자열 기반 선택 없음,
  style setter는 Background 하나뿐이다. P1/P2 finding 없음.
- 위 증거는 STA object/event/layout 및 fake timestamp tests다. 실제 Window.Show, native 입력,
  clipboard/시스템 시각 변경은 수행하지 않았다. Native 가독성/강조 강도/OS resize 검증과 구분한다.

### Native checkpoint preparation

사용자 로그인 desktop에서 정상 창을 띄우는 host PowerShell 명령:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --no-build --project 'D:\Codex\school-timetable-widget-next\src\SchoolTimetableWidget.Desktop\SchoolTimetableWidget.Desktop.csproj' -- --highlight-preview
```

도구 실행이 사용자 desktop을 보장하지 못하므로 sandbox 앱을 먼저 띄우지 않는다.
먼저 창 visibility를 확인하고 한 cycle 동안 강조 식별성/empty cell/가독성/강도,
on/off·이동 시 geometry 안정 및 Header 일치를 확인한다. 이후 가로 resize와 정상 종료를 확인한다.
이 준비 시점에는 Native/style 승인이 PENDING이었다. 실제 결과는 아래 기록한다.
승인 후 native 결과 반영, restore/build/test, final self-audit/diff check를 수행하고
사용자가 이번 milestone에 명시적으로 승인한 origin/main 일반 fast-forward push까지 진행한다.

### User native smoke — 2026-09-10

- 사용자가 위 명령을 일반 host PowerShell에서 실행했다. 실제 로그인 desktop의 정상 창을
  사용자 screenshot으로 확인했다. 도구가 sandbox/background 창을 사용자 확인용으로 실행하지 않았다.
- 첫 screenshot: 모의 시각 09:50:00, Header는 '쉬는시간 · 2교시까지 9분',
  body 강조는 0개였다. 관찰한 Break 화면에서 Header와 강조 상태가 일치했다.
- 두 번째 screenshot: 모의 시각 10:10:03, Header는 '2교시 · 종료까지 39분',
  수요일 2교시 whitespace-only cell 하나에 연한 노란색 배경이 보였다.
  빈 내용에서도 current slot이 식별되며 Header와 period가 일치하는 실제 화면 증거다.
- Screenshot 파일은 각각 사용자 첨부
  codex-clipboard-6d4b0c75-01d9-4e9b-a625-7db60862b058.png,
  codex-clipboard-831a03be-d318-4af8-a84d-74fb198bb359.png다.
  원본은 사용자 TEMP 첨부에 있으며 repository에 복사하지 않았다.
- 강조 식별성/가독성/강도와 cycle 확인 요청에 사용자는
  '나중에 커스텀 가능하면 괜찮음'이라고 답했다.
  기존 Settings milestone에서 색/opacity 등을 다룬다는 전제로 현재 후보 style을 수용한 기록이다.
  이번 milestone에 customization UI를 구현하거나 후보 색을 영구 계약으로 승격한 것은 아니다.
- 강조가 이동하는 동안 가로 폭을 줄이고 늘리는 확인 요청에 사용자는 '정상'이라고 답했다.
  사용자 resize smoke에서 눈에 띄는 layout 흔들림은 보고되지 않았다.
- X 종료 요청 뒤 사용자의 '딛음'을 닫음 응답으로 이해했고 read-only process 조회로 확인했다.
  앞서 관찰한 앱 PID 41160은 사라졌으며 남은 SchoolTimetableWidget.Desktop process는 0개였다.
  강제 종료는 하지 않았다. 계측된 timer cadence/Dispose 호출/프로세스 exit code 증거는 아니다.
- Native 결과는 위 두 화면, 사용자 style 수용, resize 확인 및 종료 관찰 범위다.
  70초 fixture의 모든 stage를 각각 native로 확인했다거나 모든 DPI/모니터에서
  pixel geometry를 계측했다고 주장하지 않는다. Exact End/Start, empty-string,
  AfterLast/Weekend 및 큰 시간 jump의 포괄적 근거는 위 자동 tests다.
- 이 범위의 native checkpoint를 통과했다. Final theme/settings와 Upcoming은 계속 DEFERRED이며
  Product Contract/Accepted ADR 변경은 없다.

### Final verification after native acceptance — 2026-09-10

- Native 수용 및 정상 종료 확인 후 restore → build --no-restore →
  test --no-build --logger "console;verbosity=normal"을 다시 실행했다. 모두 exit 0.
- Build warning/error 0, 기존 228 + 신규 48 = **276 passed**, failed/skipped 0.
  실행 log: 사용자 TEMP의 highlight-final-6346222053ad429c92dcb2a295edee67.log.
- Final source/diff self-audit에서 P1/P2 finding 없음. Day/period identity, 1/0 current,
  empty/whitespace 처리, exact End와 no-current 상태 해제, 동일 snapshot 1회 read,
  Background-only trigger와 geometry 불변을 구현 및 tests에서 재확인했다.
- Core, Product Contract/ADR, MainWindow 및 Windows content minimum 구현은 변경하지 않았다.
  Settings/theme editor, Upcoming, persistence 등 후속 범위는 추가하지 않았다.
- git diff --check 통과. 사용자 요청 22절의 명시적 승인에 따라 이 milestone을
  main에 commit하고 origin/main에 일반 fast-forward push한다.

## Future date configuration constraints — 2026-09-10

사용자 추가 요구이며 현재 Editing Foundation 구현 범위를 확대하지 않는다.

- 향후 특정 `DateOnly` 날짜의 timetable override와 period schedule override를
  서로 독립적으로 제공한다: timetable only / schedule only / both / neither.
- 날짜별 예외는 기본 `WeeklyTimetable` 또는 기본 period schedule을 파괴적으로
  수정하지 않는다. 현재 편집은 기본 weekly의 한 슬롯을 대상으로 하지만,
  text Draft/accept/cancel 편집기는 weekly singleton 또는 global state를 전제하지 않는다.
  대상 선택/반영은 소유자 경계에서 담당한다. 미래 모델을 미리 구현하지 않는다.
- 향후 하나의 `IApplicationClock` snapshot에서 날짜를 얻어 effective timetable과
  effective schedule을 resolve하고, Current Status / Highlight / Notification은
  동일한 effective day configuration을 소비한다. 각각 독립 조회하여 서로 다른
  날짜/설정 조합을 관찰하게 하지 않는다. 타입·resolver API·revision 상세는 후속 설계다.
- 향후 Header에 별도 `CurrentDateText` presentation property를 추가한다.
  표시 형식은 `yyyy년 MM월 dd일` (예: `2026년 09월 10일`)이다.
  기존 `CurrentTimeText`의 invariant `HH:mm:ss` 의미는 유지한다.
  날짜/시간/status는 같은 clock snapshot에서 생성하여 자정 경계의 불일치를 방지한다.
- 이번 milestone에는 DateOverride 모델, override UI, persistence schema,
  calendar/date selector, effective-day resolver, CurrentDateText/UI를 구현하지 않는다.

## Timetable Editing Foundation — structured value implementation

Status: **IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED (limited scope in verification record)**.
[ADR 0006](adr/0006-single-cell-in-memory-editing.md) 및 Product Contract의
2026-09-10 사용자 승인/후속 scope adjustment를 구현한다.

- Core: immutable `TimetableCellValue(SubjectText, ClassText)`, slot은 day/period와
  Value 보유. `WeeklyTimetable.WithCellValue`는 한 slot의 두 field를 한 번에 교체한다.
  초기 single Content API는 제거했다. Null 거부, 정확한 문자열 보존, unchanged no-op.
- Desktop: `TimetableCellFormatter`의 one-way DisplayText projection; 정확히 empty인
  필드만 구분 줄바꿈 생략. `CellEditSession`은 원본 value/두 Draft/typed callback만 보유한다.
  `WeeklyTimetableEditor`가 현 weekly slot을 capture하고 세션 하나를 관리한다.
  `WeeklyTimetableViewModel`은 accepted in-memory snapshot과 stable 35개 cell VM을 소유한다.
- 편집기는 두 input, 적용/취소, X/Escape 취소를 제공한다. Tab/F2/더블클릭 entry와
  표준 WPF focus 동작은 사용자 native smoke의 제한된 범위에서 확인했다. Enter multiline, no default Apply.
- Highlight는 기존 단일 clock refresh pipeline을 유지한다. Draft/Apply는 대상 identity를
  바꾸지 않으며 modal 편집 중에도 header/current slot을 같은 snapshot으로 갱신한다.
- 값과 DisplayText는 PropertyChanged 전에 함께 교체한다. Focus outline은 opacity,
  current highlight는 background만 변경한다. Content Apply 뒤 WindowContentMinimum.Refresh로
  minimum을 다시 측정한다. 이는 큰 text를 저장 format이나 화면 overflow 정책으로 제한하지 않는다.
- Persistence/Settings/override/date 표시/Bulk Input/앱 undo 시스템은 없다.
  이전 절들의 read-only/Content 구현 설명은 당시 milestone의 기록이다.
- 상세 자동 검증, self-audit 및 native 결과는
  [Editing verification](TIMETABLE-EDITING-FOUNDATION.md)을 따른다.

## Future Bulk Timetable Input — consolidated 2026-09-10

**FUTURE / PLANNED — NOT IMPLEMENTED.** 사용자의 후속 상세 요구를 이 절에 통합한다.
현재 Timetable Editing Foundation은 직접 한 셀 교과/반 편집까지다. Bulk Import,
clipboard 조사/parser/template/preview UI는 이번 milestone에 구현하지 않는다.

### Explicit modes and priority

| Mode | Priority / intent | Recognition |
| --- | --- | --- |
| A. School Timetable Import | PLANNED — preferred bulk import UX; 학교가 제공한 양식을 재작성 없이 활용 | 학교 spreadsheet의 structural signature; metadata/여러 교사 후보/ambiguity 처리 |
| B. Canonical Template Import | PLANNED — deterministic fallback / manual entry | 앱 정의 8×11 strict schema; exact header와 1–7 validation |
| C. Small Rectangular Paste | OPTIONAL / PLANNED — 작은 범위 편의 기능 | 선택 셀 anchor와 explicit paste mode; whole-table importer와 자동 혼합 금지 |

A/B/C를 한 거대한 heuristic parser로 합치지 않는다. Format recognition은 분리하고,
공통 Clipboard Table → Parsed Candidate → Validation → Timetable Import Preview →
Atomic Apply pipeline은 향후 재사용 가능하다. 이 문구는 hierarchy/API 구현 승인이 아니다.

### A. School timetable import

대표 입력은 Excel / 한셀 / Google Sheets 등의 학교 교사시간표다.
번호/교사/시수/담임/비고 metadata column이 본문 앞뒤에 있을 수 있다.
본문은 `[월 1–7][화 1–7][수 1–7][목 1–7][금 1–7]`의 35 logical slots이며,
교사 한 명이 교과 행 + 바로 대응하는 반 행으로 표현되는 구조를 고려한다.
사용자는 자신의 영역 또는 더 넓은 표를 복사하고 앱의 **학교 시간표 가져오기**에
붙여넣어 candidate/Preview를 확인한 뒤 명시적으로 Apply한다.

- Absolute 시작 column(예: 3열부터 월요일)을 hard-code하지 않는다.
- 붙여넣은 rectangular table 안에서 weekday별 1–7 sequence, 5회 반복,
  총 35 slots를 주요 structural evidence로 삼는다. 월/화/수/목/금 header는
  가능할 때 추가 evidence로 사용한다. Metadata 앞뒤 column을 허용한다.
- 학교명/교사명/과목명/반명의 의미로 위치나 slot을 추측하지 않는다.
  Multiple candidates 또는 confidence 부족이면 자동 선택/Apply와 silent guessing을
  하지 않고 사용자가 candidate를 고르거나 다른 importer mode를 사용하도록 한다.
- 명확한 교과/반 row pair만 SubjectText ← 교과 cell, ClassText ← 반 cell로 mapping한다.
  반 없는 `창체` + empty class도 허용한다. 반복된 교과/반도 각각 독립 slot이다.
- 여러 교사가 포함되면 detected teacher/timetable row-pair 후보 목록을 제시한다.
  교사명 column이 명확한 경우 label로 표시할 수 있지만 이름 자체는 구조 추론 근거가 아니다.
- Parse → Validate → Preview → Apply 순서를 지킨다. Preview에는 감지한 범위,
  선택한 teacher/row pair, 월~금 × 1–7 mapping, 각 SubjectText/ClassText 결과를 표시한다.
- Parse/validation 실패, ambiguous detection, range 불일치는 기존 timetable을 바꾸지 않는다.
  Explicit Apply만 atomic/all-or-nothing으로 반영하며 partial modification은 금지한다.

### B. Canonical template import

자동 감지가 어려운 학교 양식과 직접 입력을 위한 명시적 fallback이다.
정확히 **header 포함 8 rows × 11 columns**이며 data rows의 첫 열은 순서대로 1–7이다.
정확한 header signature:

```text
교시 | 월-교과 | 월-반 | 화-교과 | 화-반 | 수-교과 | 수-반 | 목-교과 | 목-반 | 금-교과 | 금-반
```

Strict header signature와 첫 column 1–7을 명시적으로 검증하고 정확한 canonical
form만 structured timetable로 해석한다. Header 의미를 자동 guessing하지 않는다.

향후 **시간표 양식 복사** → 빈 TSV template clipboard → Excel/Google Sheets A1에
붙여넣기 → 교과/반 작성 → 전체 8×11 복사 → **표준 양식 가져오기** → Preview → Apply를
제공할 수 있다. .xlsx 배포 없이 공통 TSV template을 우선 고려한다.
.xlsx template export는 별도 optional convenience feature다. 현재 양식을 생성하지 않는다.

### C. Small rectangular paste

선택한 timetable cell을 destination anchor로 사용한다(예: 화3에서 2×3 영역).
Ctrl+V와 clipboard TSV/table parser를 사용하는 별도 explicit paste mode 방향이다.
기존 future 요구인 **5-column simple subject mode**와 **10-column subject/class pair mode**는
이 명시적 직사각형 입력 범위의 후보로 유지한다. Canonical B의 11-column schema와 혼동하거나
A/B를 자동 추론해 전환하지 않는다. Preview 후 Apply, overflow reject(no silent clipping),
parse/validation no partial modification, all-or-nothing Apply를 지킨다.

### Clipboard text and reusable targets

- 실제 Excel/Google Sheets/유사 spreadsheet clipboard representation을 후속 조사한다.
  단순 Split(newline)/Split(tab)만으로 고정하지 않고 quoted fields/embedded newline을 처리한다.
- 가능한 clipboard semantics 범위에서 empty fields, Unicode/Korean, newline,
  leading/trailing whitespace, whitespace-only를 보존한다. Trim/Normalize 및
  IsNullOrWhiteSpace 기반 data collapse 금지. Markup-looking text는 literal plain text다.
- Format/parser와 target 선택을 분리한다. 향후 기본 weekly 또는 특정 DateOnly 예외
  timetable(예: 2026-09-17)에도 동일 parsed value/Preview/atomic Apply pipeline을 재사용한다.
- **Date-specific import target: FUTURE / depends on Date Override milestone**.
  DateOverride model, override importer UI, calendar UI는 현재 구현하지 않는다.
- 직접 편집/A/B/date-specific importer는 현재 canonical SubjectText/ClassText value를 공유할
  수 있어야 한다. Permanent Content compatibility architecture나 미래 importer/provider
  hierarchy를 선제 도입하지 않는다. 현재 구현은 값과 대상 callback의 분리까지만 제공한다.

## Future teacher profiles and groups — 2026-09-10

**FUTURE REQUIREMENT — NOT IMPLEMENTED.** 현재 Editing Foundation scope는 확대하지 않는다.
향후 여러 교사 timetable profile을 저장하고 `3학년 담임`, `과학교사`, `내가 자주 확인하는 교사`
같은 그룹 안에서 교사별 탭으로 전환하여 볼 수 있어야 한다.

- WeeklyTimetable 자체에는 teacher/group 개념을 넣지 않는다. 상위 소유 개념으로
  `TeacherTimetableProfile`(예: stable ProfileId, DisplayName, WeeklyTimetable)을 둘 수 있다.
  이 명칭/필드 예시는 future design 방향이며 이번에 타입을 생성하지 않는다.
- DisplayName은 identity/key가 아니다. 동명이인과 이름 변경을 위해 stable profile identity를 사용한다.
- TimetableGroup은 timetable data를 복제하지 않고 teacher profile reference를 보유한다.
  동일 profile은 여러 group에 동시에 속할 수 있다(예: 3학년 담임 + 과학교사).
- 학교 importer가 여러 teacher row-pair를 감지하면, 향후 한 번의 import에서 여러 profile을
  선택적으로 생성/갱신하는 구조를 고려한다. 후보 label/이름 의미로 identity를 추정하지 않는다.
  생성/기존 stable profile mapping 및 update 선택 UX는 후속 설계이며 multi-import 구현은 없다.
- DateOnly timetable override는 향후 teacher profile별로 적용할 수 있어야 한다.
  교사의 특정일 수업 변경과 학교의 특정일 PeriodSchedule/일과 변경은 별개 concern이다.
  Timetable override와 schedule override를 같은 데이터로 합치지 않는다.
- 선택된 teacher profile의 effective timetable + 해당 날짜의 effective period schedule을
  같은 IApplicationClock snapshot/date에서 resolve하여 Header/Current Highlight 등에서
  일관되게 소비한다. 기존 future effective-day configuration 원칙과 결합한다.
- Persistence 설계는 앱에 WeeklyTimetable 하나만 존재한다는 schema/coupling을 만들지 않는다.
  현재 canonical cell value와 edit session은 profile/group과 독립적이며, 명시적 weekly 소유자
  adapter는 앱 전체 singleton을 뜻하지 않는다. 새 profile selection service는 구현하지 않는다.
- 이번 milestone에 TeacherTimetableProfile, TimetableGroup, multi-tab UI,
  multi-teacher persistence/import, group comparison UI를 추가하지 않는다.

## Bulk Timetable Input — implementation, 2026-09-10

The A/B portions of the earlier future bulk section are now implemented under the
user's explicit milestone authorization. Small rectangular paste (C), date targets,
profiles/groups, semester sets and persistence remain future work.

- Core Features/TimetableImport: lossless quoted TSV table parser/writer, separate
  School structural recognizer and strict Canonical recognizer. The Timetable
  domain has no spreadsheet/platform dependency. No new project/package/framework.
- Common result: immutable complete WeeklyTimetable candidate with display-only
  source label/evidence. School candidates always require explicit selection and
  mapping confirmation; unsupported or repeated structures reject without changes.
- Desktop Features/TimetableImport: isolated session/preview/commands, owned modal
  WPF dialog, clipboard/target orchestration. Infrastructure/Windows contains the
  small OS clipboard adapter. Existing Timetable VM owns atomic whole-week swap.
- All values/projections update before notifications; stable cells preserve current
  highlight. Shared clock/status pipeline is unchanged; no additional timer.
- Context menu and view-scoped Ctrl+V; editor TextBox paste remains separate.
  Explicit template copy produces 8×11 TSV. No permanent toolbar redesign.
- Automated/object evidence and approved limited user native UX are documented in
  [Bulk Timetable Input](TIMETABLE-BULK-INPUT.md), with [ADR 0007](adr/0007-bulk-timetable-input.md).
  Earlier milestone descriptions are historical; their “not implemented” statements
  do not override this current implementation status.

## Current date and future clock/status presentation — 2026-09-10

CurrentDateText foundation: **IMPLEMENTED — AUTOMATED / OBJECT VERIFIED**.
[ADR 0008](adr/0008-clock-status-presentation.md)과 Product Contract의 후속 사용자 요청을 따른다.
이전 Phase 0.6–0.8의 두 텍스트 및 Editing Foundation의 날짜 제외 서술은 당시 기록이다.

### Current foundation and self-audit

- 기존 Desktop formatter/result/ViewModel에 `CurrentDateText`만 추가했다.
  `snapshot.Date`에서 invariant Gregorian `yyyy년 MM월 dd일`을 만들고, 기존
  `CurrentTimeText`/`StatusText`와 독립적으로 보유한다. 사용하지 않는 AM/PM/weekday
  property나 combined display string은 추가하지 않았다.
- RefreshLoop는 변경 없이 cycle당 GetSnapshot 1회 → status → countdown → formatter
  및 current slot을 계산한다. 날짜도 이 snapshot을 사용한다. ViewModel은 세 값을
  모두 교체한 후 실제 변경된 property만 알린다. 초 변경은 날짜 알림을 발생시키지 않는다.
- Core와 clock adapter는 변경하지 않았다. Domain에 ClockDisplayMode/FontSize/
  FontFamily/한국어 AM-PM/WPF Style이 없으며, 표시 변경으로 추가 clock read가 생기지 않는다.
- 현재 XAML은 date/time/status를 독립 TextBlock으로 표시하는 한 줄 standard candidate다.
  날짜/시간의 fixed slots, 기존 고정 높이, status의 남은 폭과 ellipsis를 사용한다.
  초 변경으로 status가 밀리거나 timetable row가 움직이지 않는다. MainWindow는
  feature View만 조립하므로 미래 배치는 해당 View와 Desktop formatter 안에서 교체 가능하다.
  고정 DIP 값·한 줄 배치를 architecture contract 또는 최종 preset으로 간주하지 않는다.
- Header 크기는 현재 후보에서 tick 동안 일정하다. 향후 Large Digital 등의 preset이나
  사용자 크기 설정 변경은 preferred size/re-layout을 의도적으로 바꿀 수 있다.
  일반 초 tick의 geometry 불변과 사용자 설정 변경의 re-layout을 구분한다.

### Future customization — PLANNED, not implemented

| Feature | Status | Direction / boundary |
| --- | --- | --- |
| Clock/Status presentation presets | PLANNED | Standard, Large Digital, Compact, Minimal; 이름/배치는 future UX에서 변경 가능 |
| Clock size customization | PLANNED | Time/Date/Status별 font size, font weight, alignment, spacing; preset 기본값 + 세부 사용자 override 확장 |
| 12/24-hour / seconds / AM-PM options | PLANNED | HH:mm:ss / HH:mm / h:mm:ss + AM-PM / h:mm + AM-PM; 한국어 오전/오후; 동일 snapshot 사용 |
| Date/weekday format options | PLANNED | date/weekday/status show-hide; yyyy년 MM월 dd일 dddd 또는 MM월 dd일 (ddd) 등; 현재 기본에는 weekday 없음 |
| Digital typography option | PLANNED | Normal system font 또는 digital/seven-segment 후보; 도입 시 bundled font 배포·상업적 사용 license 검증 |
| Settings persistence | PLANNED | 미래 Clock / Status Display 섹션과 값 저장; Settings object/schema/UI는 이번 범위 밖 |

Core는 snapshot/status/countdown 의미를 제공하며 Desktop이 format/font/size/layout/preset을
담당한다. 미래 property와 preset hierarchy, font dependency, Settings persistence를 지금
만들지 않는다. CurrentDateText foundation의 완료는 위 customization의 구현 완료가 아니다.
Period Schedule Editing의 전체 구현/완료를 이 추가 요구의 구현으로 대신 주장하지 않는다.

### Verification — 2026-09-10

- `dotnet build SchoolTimetableWidget.sln --no-restore`: exit 0, warning/error 0.
- `dotnet test SchoolTimetableWidget.sln --no-build --logger "console;verbosity=minimal"`:
  **413 passed**, failed/skipped 0. 기존 408 + 신규 date 사례 5개이며 관련 기존 tests도 갱신했다.
- Canonical 날짜/연초/윤일, ko-KR/en-US/ar-SA culture 독립성, 동일 local date의
  source/revision/offset 독립성, 자정/주말/시간원 전환/뒤로 보정에서 snapshot 1회 사용,
  첫 Header 알림 시 세 값 일관성 및 실제 XAML binding을 검증했다.
- 표시하지 않은 MainWindow의 content tree에서 WPF Measure/Arrange로 800→620→500→800
  폭 왕복과 초/상태/날짜 변경을 검증했다. Header 높이/status 원점 및 35셀 사각형이
  각 폭에서 유지되고 timetable measure가 tick으로 무효화되지 않음을 확인했다.
  이 폭은 테스트 예시이며 제품 minimum/preset 계약이 아니다.
- Source audit: production 직접 PC time read는 기존 fallback adapter의
  DateTimeOffset.Now 한 곳뿐이다. Feature clock read는 기존 RefreshLoop 한 곳이다.
  Core에 font/size/display mode/한국어 AM-PM/WPF 의존 추가 없음.
- 증거는 build, contract tests, unshown WPF object/binding/Measure/Arrange 및 source inspection이다.
  실제 desktop 창 표시/입력/resize gesture/DPI/font rendering 확인은 수행하지 않았다.
  날짜 추가 전 native smoke를 이번 날짜 후보의 native 검증으로 재사용하지 않는다.

### Future font sources and per-element selection — 2026-09-10

사용자 추가 요구다. 모두 **PLANNED**, optional Local Font File만 별도 future 후보이며
현재 Period Schedule Editing milestone에 font manager/Settings 구현을 추가하지 않는다.

| Source | Future resolution / boundary |
| --- | --- |
| Bundled | 앱과 함께 배포하는 resource/private font. Pretendard, DSEG7 계열 등은 예시이며 선택/라이선스 승인이 아님 |
| System | 현재 Windows에 설치된 font family, 사용자가 설치한 font도 선택 가능 |
| Online / Downloaded | 허용된 catalog(예: Google Fonts) → 사용자 요청 다운로드 → validate/cache → local/private FontFamily resolution |
| Local Font File | OPTIONAL FUTURE — 사용자가 TTF/OTF 파일을 골라 앱 전용 font로 등록; 현재 미구현 |

- Future elements `Title`, `Time`, `AmPm`, `Date`, `Weekday`, `Status`는 각각 다른
  source/family를 선택할 수 있어야 한다. 예를 들어 Time=Online/Orbitron,
  Date=Bundled/Pretendard, Status=System/맑은 고딕처럼 mixed typography를 지원한다.
  예시 family를 현재 dependency로 추가하거나 제공 가능성이 검증됐다고 주장하지 않는다.
- Header 전체에 하나의 FontFamily를 강제하지 않는다. 현재 세 텍스트의 독립
  TextBlock/binding을 유지한다. 현 View/VM/formatter에는 FontFamily를 묶는 설정이나
  전체 요소에 강제하는 local FontFamily 값이 없으며, 각 TextBlock에 향후 별도
  FontFamily/Style을 적용할 수 있다. 상속되는 system 기본 font는 초기 기본값이다.
  미사용 Title/AmPm/Weekday property 및 FontSelection 타입은 만들지 않는다.
- FontSelection의 개념적 stable identity는 SourceKind, FamilyId, FamilyName,
  optional ProviderId(Online) 등이다. 정확한 schema는 후속 설계한다.
  Machine-specific absolute font file path를 canonical setting으로 저장하지 않으며
  cache file path는 runtime resolution detail이다.
- Online asset은 사용자 선택/요청으로만 HTTPS의 known/approved provider에서 받는다.
  앱 전용 local cache 및 font availability 검증을 거친다. Remote HTTP URL을
  WPF UI FontFamily에 직접 연결하지 않는다. Startup마다 CDN을 호출하지 않으며
  normal clock rendering은 network request에 의존하지 않는다.
- 다운로드 실패가 startup 실패로 이어져서는 안 된다. Offline에서는 기존 cache 또는
  safe bundled/default fallback으로 동작한다. System font missing 또는 Online cache
  missing + network unavailable이면 fallback을 사용하고 crash/blank text/시간표·상태
  사용 불가를 방지한다. 향후 Settings는 requested font와 실제 fallback 상태를 표시할 수 있다.
- Desktop에서 사용할 수 있는 TTF/OTF/compatible OpenType asset을 우선 고려한다.
  WOFF/WOFF2 직접 지원을 추측하여 계약화하지 않는다. Online Font milestone에서
  WPF/.NET 10 지원 형식을 spike하고 확정하며 필요하면 provider의 desktop asset을 사용한다.
- 다른 PC의 custom preset도 font binary/absolute path에 종속되지 않는다. System font는
  없을 수 있으므로 fallback, Online font는 cache 없을 때 사용자 요청에 따른 provider
  재해석/다운로드 또는 fallback, Bundled font는 앱 제공 자산으로 resolve한다.
- Bundled/Online font는 도입 전에 license/배포 조건을 검증한다. Catalog는 가능한
  Provider, Family, License, Version/source metadata를 추적한다. License가 불명확한
  font를 자동 다운로드/재배포하는 구조를 만들지 않는다.
- Font source/identity/provider/cache/fallback resolution은 Desktop presentation 및
  명시적 Windows/resource/network 경계의 후속 책임이다. Core snapshot/status/countdown은
  font를 모른다. Local FontFamily를 해결한 뒤 렌더링하며 clock/tick에 font I/O를 넣지 않는다.

현재 추가한 것은 future 요구와 coupling self-audit 기록이다. Font settings UI,
system picker, online browser/downloader/cache, font persistence 및 custom preset
persistence는 구현하지 않았다. 별도 font 파일/package/network 요청도 추가하지 않았다.

## Base period schedule editing — 2026-09-10

**IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED (limited scope)**. [ADR 0009](adr/0009-editable-base-period-schedule.md)
records the user's explicit chronological ordering decision; see
[Period Schedule Editing](PERIOD-SCHEDULE-EDITING.md) for validation and native evidence.

- Core `PeriodSchedule` is a complete immutable base schedule: exactly 1..7 in input
  order, Start < End, previous End <= next Start. The constructor copies input and
  exposes a read-only collection of immutable definitions. It does not reorder or
  renumber. Existing general unordered/partial resolver contracts remain unchanged.
- Desktop `RuntimePeriodSchedule` owns one accepted schedule reference. Replacement
  accepts an already validated complete value and rejects stale baselines before
  any change. This is in-memory state, not a persistence service or global AppState.
- `PeriodScheduleEditSession`/`PeriodDraftRow` hold seven immutable identities and
  mutable text Drafts. HH:mm exact invariant parsing and Korean field/pair errors
  are Desktop responsibilities. Draft changes never construct runtime intervals.
  All fields and interval/order relations validate before one complete candidate
  is submitted. Invalid Apply keeps the Draft/dialog; successful Apply closes.
- `PeriodScheduleEditor` adapts session commit to runtime replacement and the explicit
  App-supplied `RefreshNow` callback. Session and dialog know no clock/resolver/status/
  countdown/highlight. Cancel/X/Escape do not replace state or request a refresh.
- The refresh loop's production constructor receives a snapshot accessor. Each cycle
  reads the clock once and the schedule once, then uses those same snapshots for all
  calculations and presentation/highlight publication. It never reads the source
  again during that cycle. The fixed-input convenience constructor still captures
  input once for non-editable callers and existing general resolver tests; App uses
  the live runtime accessor, so applied edits do not wait for restart or another tick.
- App initializes the runtime source from DefaultPeriodSchedule on each launch and
  injects the editor into the existing feature view. The timetable context menu adds
  `일과 시간 편집...`; no permanent toolbar row or timetable geometry change.
  Modal editor uses seven rows with read-only numbers and fourteen HH:mm TextBoxes.
- MainWindow remains composition only. Period Apply changes no SubjectText/ClassText,
  weekly content, cell editor, import parser or import target. Existing current-cell
  rendering remains background-only. The current View and 35 cell objects remain.
- Future Base + date-specific override → effective schedule must use the same clock
  date and immutable schedule boundary; this milestone implements only the base.
  Persistence, date overrides, font/settings/preset machinery and platform lifecycle
  additions are absent. Date/font requirements remain as recorded in ADR 0008.
- Explicit development `--period-preview` uses Monday 2026-09-07 13:10 plus monotonic
  process-local elapsed time, representative timetable text and the normal editor.
  The title identifies synthetic time. No system clock change; normal startup still
  uses the PC fallback. It can be combined with `--bulk-preview` for sample imports.

The previous Phase 0.7 fixed-schedule/future-editing descriptions are historical.
Current date Header retains automated/object verification and has passed limited user
native readability/resize review. Font customization remains PLANNED.

### Period schedule native acceptance — 2026-09-10

The user explicitly granted `native 승인` after direct launch, visible date/time/status,
editor entry/seven rows, invalid-time error, Cancel and physical editor-X discard,
period-5 Apply/highlight, readable period-5 Header/horizontal resize, F2 cell editing,
School/Canonical previews and restart-default checks. Exact responses and limitations
are in [Period Schedule Editing](PERIOD-SCHEDULE-EDITING.md). This does not establish
all OS input, import clipboard/Apply, all boundaries, DPI or pixel geometry coverage.
Owned native processes were normally closed by Codex without force termination.

## Optional lunch Break presentation — next Effective Day design

**PLANNED — NOT IMPLEMENTED**, user-approved future requirement, 2026-09-10.
[ADR 0010](adr/0010-optional-lunch-break-presentation.md) defines the minimal boundary.

The next Effective Day / Date Override design must pass its captured effective
period schedule to Desktop presentation alongside the same ApplicationTimeSnapshot,
resolved status and calculated countdown already used by Header/Highlight. The
formatter will additionally accept a default-false boolean option (conceptual
`showLunchBetweenPeriods4And5`). A pure label branch for Break uses PeriodNumber 4
End and PeriodNumber 5 Start from that immutable schedule; no source lookup or
hard-coded wall-clock times, duration heuristic or independent lunch timer.

| Option / captured facts | Presentation |
| --- | --- |
| OFF, any Break | Existing 쉬는시간 · {Next}교시까지 {Countdown} |
| ON, Break, Period 4 End <= local time < Period 5 Start | 점심시간 · 5교시까지 {Countdown} |
| ON, other Break | Existing ordinary Break text |
| Any option, non-Break | Existing status text; no lunch effect |
| Period 4 End == Period 5 Start | Empty lunch interval; normal InPeriod transition |

This is a Desktop presentation variant; Core retains exactly five states, unchanged
countdown semantics and highlight behavior. Base schedule edits and future date
schedule overrides affect the same effective endpoints automatically. No additional
CurrentStatusResult field or Lunch kind is needed. The composition layer supplies
the option, and a later Settings checkbox can expose it. Full Settings, preference
persistence, effective-day resolver and runtime lunch formatting are not implemented
by this documentation change. Existing runtime behavior remains OFF/ordinary Break.

Boundary cases and one-clock/one-schedule regression checks are future verification
criteria in ADR 0010, not tests claimed as executed. The prior milestone records
of ordinary Break describe the default and remain valid.

## Effective Day / Date Overrides — implementation, 2026-09-10

**IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED.** [ADR 0011](adr/0011-effective-day-and-date-overrides.md)
records the user-approved 7-cell complete snapshot and displayed-provenance editing rules.
Earlier foundation/future sections are historical; this section supersedes their exclusion
of Effective Day, date overrides and the ADR 0010 runtime lunch option.

Core Features/SchoolDays contains DayTimetable (seven immutable SubjectText/ClassText
values), DateSpecificOverride (DateOnly + independently optional timetable/schedule),
EffectiveDayConfiguration and pure EffectiveDayResolver. The resolver takes a date and
captured values, projects only that weekday into the base week, and returns effective
schedule/provenance. It has no WPF, Korean strings, clock read or runtime dictionary.
PeriodSchedule's existing complete chronological invariant is shared by base/date schedules.

Desktop Features/DateOverrides owns a private DateOnly map in RuntimeDateOverrides.
TryReplace checks the captured entry before one full-reference replacement/removal. The
minimal API supports all component removal through validated replacement; no generic
repository or persistence API. DateOverrideEditSession owns independent Drafts, reuses
PeriodScheduleEditSession's candidate validation without committing it, then invokes its
owner once with both validated values. Null/null removes the entry. Disabled Draft fields
are ignored. DateOverrideEditor adapts fixed-date whole-entry and single-cell transactions.
Cell-only edits compare the captured timetable snapshot, preserve the current schedule
component and reject removed/replaced timetable targets. The date adapter never reads clocks.

WeeklyTimetableViewModel keeps its Base CommittedTimetable separate from DisplayedOverride
and its stable 35 cell presentations. Effective publication installs provenance and all
values/projections before cell notifications. WeeklyTimetableEditor checks the displayed
component and weekday, captures the typed target at opening, and delegates to the date
adapter when appropriate. Text equality and period-schedule presence never select a target.
Base editing/import projects through the still-current date timetable; it cannot overwrite
that override. Explicit Base labels remain in single-cell and import dialogs.

App composes runtime state and one effective-date function. CurrentStatusRefreshLoop reads
one clock snapshot, invokes that function once, and shares the result's schedule with Core
status/countdown and Desktop formatting. It publishes the same result's grid/provenance and
current slot. Reentrant refresh publication is suppressed; there is no nested message pump
or await. Existing fixed/general schedule overloads remain for non-effective callers and
partial/unordered Core contract tests; production uses the effective overload.
ContentChanged requests Windows content-minimum measurement only for changed display text,
including midnight content transitions. Ordinary ticks, provenance-only changes, lunch
labels and highlight do not remeasure the grid through this hook.

LunchPresentationOption is default-OFF run-local Desktop state. Its menu requests the same
RefreshNow on changes. The formatter uses only supplied snapshot/status/countdown/effective
periods and selects the label by period 4/5 identity. No Lunch Core kind, hard-coded time,
second timer, schedule query or Settings infrastructure. CurrentDateText/CurrentTimeText/
StatusText stay separate. Existing font/preset future requirements remain unchanged.

Timetable values have no global/profile identity and can move under teacher-profile
ownership; school-day PeriodSchedule remains a separate value/concern. The combined runtime
entry provides today's atomic editing boundary without fixing a future persistence/profile
schema. Profiles/groups, semester sets, date imports and persistence are PLANNED.

See [Date Overrides verification](DATE-OVERRIDES.md) for tests, self-audit and native limits.

## Native profile persistence — 2026-09-11

**IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED (limited scope).** [ADR 0012](adr/0012-native-local-profile-persistence.md)
and [Persistence](PERSISTENCE.md) supersede the native v1 persistence/recovery deferrals
and prior run-local descriptions. Full backup/restore and migration remain future work.

Desktop Features/Persistence contains only the immutable ProfileSnapshot, load outcome,
ProfileSession save-before-publish boundary and ProfileRuntime composition of existing
feature owners. Core values/invariants remain serialization- and UI-independent.
Infrastructure/Persistence owns strict System.Text.Json DTO mapping and the leased
JsonProfileStore; Infrastructure/Windows/ProfileLocation resolves per-user LocalAppData.
No new project/package, generic filesystem layer, global AppState, background queue,
Task.Run or DI framework. Future teacher/profile collections can migrate schema 1's
profile envelope without giving WeeklyTimetable global identity.

Existing owners accept optional typed persistence callbacks. Production ProfileRuntime
always injects them; independent in-memory tests and import previews need no store.
Callbacks build a full immutable candidate from the latest persisted snapshot. Stale
feature baseline checks precede callback invocation. Save succeeds before owner reference
replacement and notifications; editor sessions show the owner's precise save error.
Date components/removal commit once. Lunch changes save before its property notification
and shared refresh. Effective calculations remain on the single Application Clock.

App loads/validates before MainWindow construction, composes all owners from the loaded
snapshot, then starts shared refresh before Show. Degraded temporary defaults have a
persistent notice and reject every durable callback. OnExit only disposes refresh and
storage lease; it never saves. A DEBUG-only explicit --dev-profile-directory beneath
TEMP enables restart smoke; existing preview modes select unique TEMP storage by default.
Saved preview data wins over the sample seed on restart; production paths are not used.

The store holds profile.lock exclusively for its lifetime, compares expected profile
bytes before replacement, writes a unique same-directory temp, flushes/closes it and
renames it over profile.json. Cleanup failure does not turn a successful rename into a
failed transaction. Original corrupt/unsupported files and rejected external changes
are never automatically repaired or replaced. No previous/history file is generated.
Writer exclusion is storage safety only; P6 activation and tray remain PLANNED.

## Future Week Navigation / Date Header — 2026-09-11

**PLANNED — NOT IMPLEMENTED.** 사용자가 명시적으로 추가한 향후 Weekly Timetable View
요구사항이다. 현재 Local Persistence Foundation의 구현·저장 schema·검증 범위를 확대하지
않는다. 아래는 future presentation semantics이며 현재 View가 이미 주간 browsing이나
다섯 날짜의 개별 effective projection을 제공한다는 뜻이 아니다.

### Displayed week and date headers

- 앱 시작 시 공통 Application Clock의 현재 날짜가 속한 주의 Monday–Friday를 표시한다.
  weekday column은 계속 월–금 5개, 각 column의 교시는 기존 1–7을 유지한다.
- 각 요일 헤더 위에 그 column에 대응하는 실제 `DateOnly`를 표시한다. 날짜 identity와
  표시 문자열을 구분하며, compact 기본 format 후보는 zero-padding을 요구하지 않는
  `M/d`다. 예: 2026-09-07 주의 `9/7`, `9/8`, `9/9`, `9/10`, `9/11`.
  정확한 format customization은 future Display Settings에서 확장할 수 있다.
- 왼쪽 화살표는 이전 주, 오른쪽 화살표는 다음 주로 이동한다. 이동 단위는 정확히
  7일이다. 월/연도 경계에서도 weekday를 따로 추정하지 않고 이동한 실제 날짜를 사용한다.
  버튼 배치·크기·색상 등 구체적 visual design은 이 요구에서 확정하지 않는다.
- 표시 중인 주는 Desktop browsing state다. 현재 native profile snapshot/schema에
  추가하지 않는다. 향후 이 View가 구현되어도 기본 재시작 동작은 현재 주로 돌아오는
  것이다. `마지막으로 보던 주 기억`은 별도 future 설정 후보이며 승인된 기본 기능이 아니다.

### Effective timetable per displayed date

- 표시 주의 다섯 날짜를 각각 resolve한다. 각 column은 그 실제 날짜의 complete
  seven-cell Date Timetable Override가 있으면 이를 사용하고, 없으면 Base WeeklyTimetable의
  해당 weekday 값을 사용한다. 예: 2026-09-10 override는 그 날짜의 목요일 column에만 적용된다.
  다른 주의 목요일이나 다른 날짜에 같은 override를 적용하지 않는다.
- 현재 구현의 오늘 한 날짜 projection을 다섯 column 전체의 future 결과로 간주하지 않는다.
  미래에는 열마다 DateOnly, 표시 값, Base/date provenance를 함께 일관되게 제공해야 한다.
  정확한 ViewModel/type/cache/refresh 구조는 구현 milestone에서 정하며 지금 선제 추가하지 않는다.
- Date Period Schedule Override는 timetable content와 독립적이다. 주간 browsing은 실제
  오늘의 effective schedule/status 계산 대상을 바꾸지 않는다.

### Actual current state, highlight and today indication

- `CurrentDateText`, `CurrentTimeText`, Current Status, Countdown은 계속 실제 공통
  Application Clock과 실제 오늘의 effective schedule을 기준으로 한다. 이전/다음 주를
  보더라도 clock을 선택 날짜로 바꾸거나 Header를 browsing 날짜의 상태로 바꾸지 않는다.
  Lunch presentation도 이 실제 현재 상태/effective schedule을 따른다.
- Current Highlight는 실제 오늘 DateOnly의 current period cell만 의미한다. 표시 주에
  오늘이 포함되고 현재 수업 중이면 해당 날짜/교시 cell을 강조한다. 오늘을 포함하지 않는
  주에는 current timetable highlight가 없다. 같은 weekday/period라는 이유로 다른 주의
  cell을 current로 강조하지 않는다. 기존 Break/Weekend 등의 no-current 원칙도 유지한다.
- 표시 주에 오늘이 포함될 때 date/weekday header에서 오늘 column을 구분하는 방향을
  고려한다. 이 today indicator는 현재 수업 cell 강조와 별개의 날짜 표시 의미다.
  정확한 색상/style/도형은 future visual design에서 결정한다.

### Editing provenance and import

- F2/double-click은 해당 displayed date cell의 명시적 source를 따른다. Date Override에서
  온 cell은 그 DateOnly override를 편집하고, Base에서 온 cell은 Base timetable을 편집한다.
  문자열 일치/내용이나 다른 주의 동일 weekday로 source를 추측하지 않는다.
- 기존 open-editor target capture, stale rejection 및 Apply/Cancel 규칙을 유지한다.
  편집을 연 뒤 browsing 날짜나 실제 날짜가 바뀌어도 열린 Draft의 대상을 암묵적으로
  바꾸지 않는다. Base 편집을 자동으로 date override 생성으로 바꾸지도 않는다.
- School Timetable Import와 Canonical Template Import는 계속 Base WeeklyTimetable 전용이다.
  표시 중인 주/날짜가 바뀌어도 Bulk Import target을 자동으로 date override로 바꾸지 않는다.

### Unconfirmed UX candidates and later verification

`오늘` 버튼, 오늘 주로 즉시 돌아가기, 날짜 클릭, 주 선택 calendar와 마지막으로 본 주 기억은
**FUTURE CANDIDATES — NOT DECIDED / NOT IMPLEMENTED**다. 이번 requirement로 버튼이나
calendar 동작을 확정하지 않는다.

향후 검증 대상은 시작 주/정확한 ±7일 이동, 월·연도·윤일 경계, 다섯 날짜의 독립 override
fallback/provenance, 다른 주 browsing 중 실제 Header/Countdown 유지, 오늘 포함 여부에 따른
highlight/today indicator, 편집 대상 고정, Base-only import, 재시작 현재 주 복귀다.
이는 미래 acceptance criteria이며 현재 585 tests/native smoke로 검증 완료했다고 주장하지 않는다.
현재 구현과 Accepted ADR 0011/0012의 동작은 그대로이며 후속 구현 시 관련 계약/ADR을 함께 갱신한다.

## Week Navigation implementation — 2026-09-11

**IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED.** This implements the future requirement above;
its PLANNED statements are historical. [ADR 0013](adr/0013-viewed-week-and-date-header.md)
and [Week Navigation](WEEK-NAVIGATION.md) define the current behavior/evidence boundary.

WeeklyTimetableViewModel retains Base commitment and 35 stable cells. Its Navigation
partial owns nullable pre-start ViewedWeekStart, two RelayCommands, displayed columns,
actual date/slot facts and a configured date-map lookup. First refresh initializes the
Monday before MainWindow.Show; no View/MainWindow date calculation is added. Each column
owns immutable date/source identity and seven shared cell references; IsToday is observable.

The previous DisplayedOverride/ApplyEffectiveDay single-date projection API is removed.
The grid resolves each of five DateOnly keys independently using the existing complete-day
projection function. Base commits reproject the cached date components; successful date
editor commits refresh relevant columns through ProfileRuntime after durable publication.
Today schedule/status refresh remains separately gated by actual CurrentDate in App.

Ticks update IsToday and date-gated current-cell facts without fetching date columns or
allocating 35 new cells. Navigation performs no clock read and no save. Source correction,
midnight, Sunday→Monday and weekends do not force a navigation. UI dates use M/d, actual
Header date keeps its existing full format. Import preview retains its own weekday-only
layout and Base target. No persistence DTO/schema, Core model or new timer is introduced.

Previous occupies the existing period-header corner; Next uses a narrow trailing header
slot. The five headers and body share one equal-column region. Today and current-cell
triggers change backgrounds only. Exact spacing/color remain native candidates.

Native UX was explicitly approved on 2026-09-11. The user defers detailed arrow/header
visual polish to a future styling/display milestone; it is not a blocker here. The observed
native scope and limits are recorded in [Week Navigation](WEEK-NAVIGATION.md).

## Display Settings implementation — 2026-09-11

**IMPLEMENTED — NATIVE REVIEW CONFIRMED**. [ADR 0014](adr/0014-display-presets-and-schema-v2.md)
and [Display Settings](DISPLAY-SETTINGS.md) supersede prior display PLANNED entries.

Desktop Features/DisplaySettings contains immutable configuration/preset defaults, per-element
Drafts, a P2 transaction session, runtime preview ownership, and the display-only settings view.
Infrastructure/Windows/SystemFontCatalog enumerates and resolves WPF system family names,
including fallback without replacing canonical identity. No Core, global AppState, provider
framework, new package or downloaded font is introduced.

CurrentStatusHeaderFormatter adds same-snapshot weekday/AM-PM/12-hour fields; the independent
date/time/status values remain. ViewModel caches the last immutable formatted facts and applies
display choices without clock reads. Header view reconfigures its retained controls on Display
changes only, reserving slots for stable ordinary ticks; oversized header width is scrollable.
App wires the Display owner to that ViewModel and the existing WindowContentMinimum boundary.
The shared clock/status/highlight refresh loop is unchanged.

ProfileSnapshot carries validated immutable Display. ProfileSession has a typed SaveDisplay
callback and preserves Current.Display in all other saves. Separate strict v1 and v2 storage
DTOs ensure v1 upgrade in memory only, followed by v2 on the next successful user save.
Preview never enters a save of another feature. JSON shape, UI, fallback, tests and evidence
are detailed in Display Settings; native UX acceptance was confirmed on 2026-09-11.
