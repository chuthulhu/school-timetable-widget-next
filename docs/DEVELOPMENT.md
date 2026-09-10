# Development Setup

## Prerequisites

- Windows x64
- Git
- .NET 10 SDK: `global.json`의 SDK 선택 정책을 만족하는 x64 설치

Visual Studio는 필수 prerequisite가 아니다. CLI restore/build/test를 기준으로 한다.
SDK는 개발 PC마다 필요하고 NuGet dependencies와 정확한 직접 참조 버전은 project가 관리한다.

## New development PC

```powershell
git clone https://github.com/chuthulhu/school-timetable-widget-next.git
Set-Location school-timetable-widget-next
.\scripts\bootstrap-dev.ps1
```

환경 점검만 할 때:

```powershell
.\scripts\check-dev-env.ps1
```

두 script는 Windows PowerShell 5.1과 PowerShell 7에서 사용한다. 다른 작업 디렉터리에서
절대 경로로 실행해도 script 위치를 기준으로 repo와 `global.json`을 찾는다.
조직의 execution policy가 script 실행을 막으면 해당 정책에 맞게 처리한다. Script는 policy를 바꾸지 않는다.
이 PC의 Windows PowerShell 기본 정책은 script 실행을 막았다. 허용된 로컬 코드 검증에는
자식 프로세스에서만 적용되는 다음 명령을 사용했다. 시스템/사용자 execution policy를 바꾸지 않는다:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\bootstrap-dev.ps1
```

## SDK policy

```json
{
  "sdk": {
    "version": "10.0.400",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

10.0.400 이상인 설치된 stable 10.0 SDK 중 최신 feature band/patch를 허용한다.
낮은 SDK, prerelease, .NET 11 또는 다른 minor 버전으로 암묵적으로 전환하지 않는다.
[공식 global.json 정책](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json#rollforward)을 따른다.

## What the scripts do

`check-dev-env.ps1`은 Windows x64, Git 실행, global.json, solution/project 파일,
선택 가능한 x64 SDK, WPF application template을 읽기 전용으로 점검한다.
PATH의 `dotnet.exe`와 표준 x64 설치 위치를 함께 탐색하고 repo에서 `dotnet --version`을 실행해
실제 SDK resolver가 global.json을 만족하는지 확인한다. 실패한 PATH 후보는 표준 설치 경로로 재확인한다.
Restore assets 존재 여부는 참고 정보이며 freshness 또는 NuGet 접속 성공의 증거가 아니다.
이 점검은 restore/build/test, SDK 설치, 네트워크/credential/PATH/system 설정 변경을 수행하지 않는다.
.NET CLI 자체의 최초 실행 bookkeeping은 있을 수 있다.
`-PassThru`는 bootstrap이 사용하는 진단 객체(ExitCode, DotnetPath, SdkVersion, Message)를 반환한다.

| Check exit code | 의미 |
| --- | --- |
| 0 | 로컬 개발 prerequisite 준비됨; NuGet/build/test는 별도 검증 필요 |
| 1 | 예기치 않은 점검 실패 |
| 2 | Windows/x64/Git/global.json/repository prerequisite 실패 |
| 3 | 요구되는 x64 SDK 없음 또는 사용할 수 없음 |
| 4 | SDK는 선택되지만 WPF application template 사용 불가 |

`bootstrap-dev.ps1`은 먼저 위 점검을 실행한다. SDK가 필요한 경우에만 winget을 찾고
설치 명령을 표시한 뒤 기본적으로 `y` 또는 `yes` 입력을 받아 설치한다. 빈 응답/거절/입력 실패로는 설치하지 않는다.
명시적으로 설치를 승인하려면 `-Yes` 또는 `-Y`를 사용할 수 있다.
이 parameter는 SDK 설치 동의이며 winget/Windows 자체 UI나 권한 요구까지 자동 승인한다는 의미는 아니다.

```powershell
.\scripts\bootstrap-dev.ps1 -Yes
```

실행할 설치 명령:

```powershell
winget install --id Microsoft.DotNet.SDK.10 -e --source winget
```

Git/global.json 등의 문제가 있으면 SDK 설치로 우회하지 않는다. winget이 없으면 다른 installer를
자동 다운로드하지 않고 수동 조치를 안내한다. SDK 설치 뒤에는 모든 prerequisite를 재확인한다.
현재 shell의 PATH가 오래됐더라도 `C:\Program Files\dotnet\dotnet.exe`를 재탐색하여 계속할 수 있다.
Program Files 위치가 다른 PC에서는 해당 표준 x64 Program Files 경로를 사용한다. PATH 자체는 변경하지 않는다.

점검 성공 후 solution에 다음을 순서대로 실행한다:

```powershell
dotnet restore SchoolTimetableWidget.sln
dotnet build SchoolTimetableWidget.sln --no-restore
dotnet test SchoolTimetableWidget.sln --no-build
```

실패하면 다음 단계로 넘어가지 않으며 단계와 exit code를 출력한다.
Prerequisite 실패는 check의 exit code, 설치 거절/동의 입력 실패는 5,
winget/restore/build/test 실패는 원래 명령의 exit code, 기타 예외는 1이다.
자동 설치나 의존성 복원 실패를 성공으로 처리하지 않는다.
네트워크/DNS/proxy/token/credential을 변경하거나 target framework를 낮추지 않는다.

## Manual fallback

SDK가 필요하면 위 winget 명령으로 설치한다. winget이 없으면
[Microsoft .NET 10 다운로드](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)에서
Windows x64 SDK를 설치한 다음 global.json 요구사항을 확인한다.
Repo root에서 다음을 실행한다:

```powershell
dotnet --info
dotnet sln list
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

현재 PowerShell의 PATH에 dotnet이 없으면 새 shell 없이 절대 경로를 사용할 수 있다:

```powershell
$dotnetExe = 'C:\Program Files\dotnet\dotnet.exe'
& $dotnetExe --info
& $dotnetExe restore
& $dotnetExe build --no-restore
& $dotnetExe test --no-build
```

NuGet 접근이 막히면 실패 출력과 exit code를 확인한다. Codex 실행 환경에만 제한이 있으면
host PowerShell에서 `bootstrap-dev.ps1`을 실행한다. 해결을 위해 시스템 네트워크 설정을 자동 변경하지 않는다.

## Solution and test scope

| Project | Target framework | Project reference / direct package |
| --- | --- | --- |
| SchoolTimetableWidget.Desktop | net10.0-windows | Core / CommunityToolkit.Mvvm 8.4.2 |
| SchoolTimetableWidget.Core | net10.0 | 없음; WPF/Toolkit 독립 |
| SchoolTimetableWidget.Tests | net10.0 | Core / xunit.v3.mtp-off 4.0.0, xunit.runner.visualstudio 4.0.0, Microsoft.NET.Test.Sdk 18.9.0 |

SDK 10.0.400의 기본 `dotnet new xunit`은 xUnit v2 2.9.3을 생성했다.
[공식 v3 전환 안내](https://xunit.net/docs/getting-started/v3/migration)에 따라
`xunit.v3.mtp-off`와 executable test project를 사용했다. 외부 template는 설치하지 않았다.
`xunit.v3.mtp-off`의 4.0.0은 v3 framework 계열의 package version이다.
[패키지 메타데이터](https://www.nuget.org/packages/xunit.v3.mtp-off/4.0.0),
[Toolkit 버전](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2)은 2026-09-08 확인했다.

`dotnet test`는 VSTest와 xUnit adapter를 사용한다. 공식 `mtp-off` package로 MTP 통합을 제외했다. 기본 `xunit.v3` package의 MTP 2 통합은 .NET 10에서 VSTest 경로를 거부하므로 두 runner를 섞지 않는다. [공식 선택 안내](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform#choosing-the-microsoft-testing-platform-version).
Template의 Class1/UnitTest1과 coverage collector를 제거했다. 최초 Phase 0 skeleton에서는 제품 테스트가
0개였으며 아래 Phase 0 검증 표는 그 당시 기록이다. Phase 0.2에는
`Time/ApplicationClockContractTests.cs`와 Tests 내부 `Time/FakeApplicationClock.cs`를 추가했다.
고정 DateTimeOffset과 fake 교체로 시각/offset 보존, reference identity, 자정 및 source 전환 전후
snapshot 전달을 검증한다. 실제 system clock 변경, native UI/input 또는 네트워크를 사용하지 않는다.
Tests는 Core만 참조하므로 Desktop adapter와 WPF startup의 실제 실행 검증은 포함하지 않는다.
직접 system time 읽기는 Desktop의 `Infrastructure/Time/PcFallbackApplicationClock.cs`만 허용한다.
Core snapshot 필드와 composition/source/revision 의미는 [Architecture](ARCHITECTURE.md#phase-02-application-clock-foundation)를 따른다.

## Developer bootstrap vs. end-user installation

일반 최종 사용자는 .NET SDK를 설치할 필요가 없다. 향후 사용자 배포는 self-contained installer가
후보이며 최종 runtime 배포 방식과 installer/updater 기술은 아직 DEFERRED다.
개발 bootstrap은 개발 PC에서 코드를 빌드하기 위한 도구이고 사용자 installer와 별개다.
이번 Phase 0에서 사용자 installer를 구현하거나 선택하지 않았다.

## Bootstrap control-flow checks

제품 테스트와 별도로 bootstrap의 설치 동의와 실패 경계는 의존성 없는 PowerShell 검사로 재현한다:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\bootstrap-dev.Tests.ps1
# PowerShell 7:
pwsh.exe -NoProfile -File .\tests\bootstrap-dev.Tests.ps1
```

이 검사는 `dotnet test`에 포함되지 않는다. 각 실행은 고유 TEMP fixture에 script/project 설정을 복사하고
Git/dotnet/winget 탐색과 동의 응답을 stub으로 대체한다. 실제 installer, SDK command 또는 native UI를
실행하지 않는다. 진단 fixture 경로를 출력하며 실제 설치 성공의 증거로 해석하지 않는다.
18개 case는 준비된 환경, Git/winget 부재, 잘못된 global.json, 설치 거절/입력 실패/명시적 동의,
`-Yes/-Y`, 설치 실패/설치 뒤 SDK 여전히 없음, 잘못된 SDK major/낮은 버전/prerelease,
WPF 부재, restore/build/test 단계별 실패를 검증한다. 다음 단계 차단, 원래 exit code,
설치 명령 및 작업 디렉터리 복원도 확인한다.

## Phase 0 verification — 2026-09-08

| 검사 | 실행 결과 / 증거 범위 |
| --- | --- |
| SDK / template / solution | 10.0.400 x64, WPF template 확인. `dotnet new sln --help`의 `--format sln` 사용. `dotnet sln list`에 지정된 3 projects만 존재 |
| Restore | 성공, exit 0. 기본 Codex sandbox의 NuGet.Config 접근 제한 후 승인된 실행 권한으로 기존 NuGet 설정을 사용해 복원 |
| Debug build | `--no-restore` 성공, warning 0 / error 0 |
| Test runner | `--no-build` exit 0. 상세 로그에서 xUnit VSTest Adapter 4.0.0의 Discovering/Discovered 확인. 제품 테스트 0개, no-tests 진단 예상 |
| 실제 bootstrap | SDK가 이미 설치된 PC에서 repo 밖 TEMP 작업 디렉터리로 Windows PowerShell 5.1 실행. 절대 dotnet 경로로 restore → build → test 성공 |
| Check script | Windows PowerShell 5.1 / PowerShell 7 모두 exit 0. 읽기 전용 점검 전후 모든 non-ignored repo 파일 SHA-256 동일 |
| Bootstrap 분기 | PowerShell 5.1 / 7 각각 18개 모의 case 통과. 실제 SDK 설치는 실행하지 않음 |
| Core 독립성 | csproj 참조 검사, source 검색, MSBuild 평가 확인. Core의 project/package reference 없음, framework reference는 Microsoft.NETCore.App만 존재 |
| 참조 / 시간 규칙 | Desktop → Core, Tests → Core만 존재. production source에 직접 DateTime.Now / DateTimeOffset.Now 없음 |

Windows PowerShell 5.1의 기본 execution policy는 script 실행을 막아 검증 자식 프로세스에만
`-ExecutionPolicy Bypass`를 사용했다. 시스템/사용자 policy, PATH, 네트워크 설정은 변경하지 않았다.
실제 SDK 설치, native WPF UI/input/IME/DPI, 제품 기능 및 Release build는 이 단계에서 검증하지 않았다.

## Phase 0.2 verification — 2026-09-08

| 검사 | 실행 결과 / 증거 범위 |
| --- | --- |
| Environment | Windows PowerShell의 `check-dev-env.ps1` exit 0. x64 SDK 10.0.400, WPF template 및 세 project 확인 |
| Restore | `dotnet restore` exit 0; 모든 프로젝트 최신 상태 |
| Debug build | `dotnet build --no-restore` exit 0; warning 0 / error 0 |
| Contract tests | `dotnet test --no-build --logger "console;verbosity=normal"` exit 0; 13 passed, 0 failed, 0 skipped |
| Snapshot facts | PC offset +09:00/00:00/-07:00과 subsecond ticks 보존 3 cases; UTC 날짜와 다른 KST 날짜/시각/source/revision 1 case |
| Reference identity | 같은 instant의 fallback→sync, sync→새 sync reference, sync→fallback 구분 3 cases; 시간 진행과 revision 유지 1 case |
| Consistent calculation | 자정 및 자정과 source 전환에서 한 번 캡처한 snapshot을 관련 계산에 전달 2 cases. 테스트용 소비자이며 실제 current-period 기능은 없음 |
| Invalid snapshot | 알 수 없는 source, 음수 revision, synchronized source의 비-KST offset 거부 3 cases |
| Architecture | Project reference는 Desktop→Core, Tests→Core만 존재. Core MSBuild 평가에서 project/package reference 없음, framework는 Microsoft.NETCore.App만 존재. Core source의 WPF/Toolkit/Desktop 참조 없음 |
| Direct system reads | bin/obj 제외 production .cs에서 DateTime.Now, DateTimeOffset.Now, DateTime.UtcNow, DateTimeOffset.UtcNow 검색. PcFallbackApplicationClock.cs의 DateTimeOffset.Now 1건만 존재 |

최종 source review에서 clock의 책임을 시간 사실과 reference identity로 제한했음을 확인했다.
Snapshot은 get-only이고 날짜/시각은 같은 DateTimeOffset에서 파생한다. Production fake, 미래 NTP client
abstraction, formatting/rounding 또는 학교 상태 계산을 추가하지 않았다.
PC fallback의 비-KST offset은 그대로 보존하며 추가 timezone 정책은 도입하지 않았다.
이번 범위의 제품 의미 확인 blocker는 없다. 실제 sync correction/concurrency/resume 정책은 후속 spike 범위다.

WPF 창을 실행하거나 활성화하지 않았고 native input, system clock mutation, KRISS/NTP 통신은 수행하지 않았다.
Desktop adapter/composition은 build와 source inspection으로 확인했다. Contract tests는 Core와 fake 증거이며
실제 OS adapter 실행, native startup 또는 동시 sync 전환 검증으로 해석하지 않는다.

## Editing Foundation native checkpoint — 2026-09-10

한 셀의 교과/반 편집, in-memory Apply/Cancel foundation이다.
Persistence, Bulk Import, Date Override, 날짜 표시는 구현하지 않았다.

Codex가 먼저 정상 앱을 직접 실행하고 process 상태를 확인한다. 사용자에게는 실제 창 표시와
시각적/native UX를 확인한다. 아래 host PowerShell 명령은 현재 실행 권한/desktop isolation
등의 실패를 확인한 경우에만 사용하는 fallback이다. 과거 isolation만으로 수동 실행을 요구하지 않는다.

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --no-build --project 'D:\Codex\school-timetable-widget-next\src\SchoolTimetableWidget.Desktop\SchoolTimetableWidget.Desktop.csproj' -- --highlight-preview
```

모의 시각 70초 순환은 기존 공통 application clock 주입이다. 시스템 시간은 바꾸지 않는다.
대표 preview에는 월3 교과/반 pair 및 화3 class-only도 포함한다. 편집은 정상 실행에서도 가능하다.

한 단계씩 실제 창 visibility → 한 셀 더블클릭 → 두 필드 한글/Enter/Tab 연속 입력과 적용 →
재열기/F2 → 취소/X/Esc → 강조 이동과 공존/resize → X 종료 확인 순서로 진행한다.
Focus/IME 확인 시 입력과 Tab/버튼 이동은 앱 안에서 연속 수행한 뒤 chat으로 돌아온다.
자동 object/event tests를 native 동작 통과로 취급하지 않는다.
결과와 범위는 [Editing verification](TIMETABLE-EDITING-FOUNDATION.md)에 기록한다.
