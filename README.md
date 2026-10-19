# School Timetable Widget Next

교사가 주간 시간표와 현재 수업을 확인하고 편집하는 Windows 데스크톱 위젯입니다.
WPF + .NET 10 LTS + CommunityToolkit.Mvvm으로 개발 중이며, hosted runtime service가 아닙니다.
**IN_DEVELOPMENT**: 주요 기능과 제한된 native UX 검증은 완료했지만 출시용 installer/updater는 아직 없습니다.

## 현재 기능

- 월–금 × 7교시 독립 35셀의 교과/반 편집, School/Canonical 일괄 가져오기와 미리보기.
- 일과 시간 편집, 날짜별 시간표/일과 예외, 선택적 점심 표시, 주간 탐색.
- 공통 Application Clock의 현재 시각·날짜·수업 상태·countdown·현재 셀 강조.
  PC local fallback으로 즉시 시작하고 KRISS NTP 동기화 성공 후 KST를 사용합니다.
  Windows 시스템 시계는 변경하지 않습니다. [정책과 검증 범위](docs/KRISS-TIME-SYNC.md).
- 표시 프리셋과 사용자 프리셋, 요소별 글꼴/크기/형식, bundled/system/online 글꼴,
  한 프리셋의 `.stwpreset` 가져오기/내보내기.
- profile v5 저장, 전체 학기를 포함하는 `.stwbackup` 백업/복원,
  손상된 원본 보존 및 명시적 복구.
- 여러 Semester Sets의 생성·전환·이름 변경·비활성 학기 삭제.
- 여러 줄 내용의 화면 맞춤, 창 위치/선호 크기 저장, tray 숨김/표시, 단일 인스턴스,
  명시적 Windows 자동 시작 등록. **X는 숨김, tray의 종료는 완전 종료**입니다.

Teacher Profiles / Groups는 [foundation 문서](docs/TEACHER-PROFILE-GROUP-FOUNDATION.md)만 확정했으며
구현은 **DEFERRED**입니다. 현재 제품은 implicit single-teacher로 동작합니다.

기존 시간표/교시 JSON 변환, `.stwshare` 선택 공유와 일괄 저장은
[데이터 처리 기반](docs/DATA-INTERCHANGE.md)을 구현했습니다.
**사용자용 메뉴·미리보기·파일 대화상자는 아직 연결하지 않았습니다.**

정확한 구현/미구현 범위는 [Feature Map](docs/FEATURE-MAP.md),
저장 데이터의 소유권은 [Continuity](docs/CONTINUITY.md)를 따릅니다.

## 시작하기

Windows x64, Git, [global.json](global.json)을 만족하는 .NET 10 SDK가 필요합니다.
저장소 root에서:

```powershell
dotnet --info
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

새 PC 준비, 선택적 bootstrap, 격리된 TEMP profile 실행과 정상 실행 방법은
[Development](docs/DEVELOPMENT.md)를 읽으세요.
현재 자동 검증은 **Windows 1,263 tests, Linux Core 299 tests, 실패/skip 0**이며,
Windows build warning/error 0, 격리된 bootstrap 검증 18개 통과입니다.
검증 commit과 증거 한계는 [Continuity](docs/CONTINUITY.md)에 기록합니다.

Linux에서도 순수 데이터 테스트를 실행할 수 있습니다. 이 실행은 Windows 전체 테스트를 대신하지 않습니다:

```bash
dotnet test tests/SchoolTimetableWidget.Tests/SchoolTimetableWidget.Tests.csproj -p:CoreOnlyTests=true
```

## 저장소만으로 이어가기

1. [CONTINUITY](docs/CONTINUITY.md): 현재 상태, 읽기 순서, 안전 경계, 재검증.
2. [Product Contract](docs/PRODUCT-CONTRACT.md)와 [Accepted ADR index](docs/adr/README.md).
3. [Architecture](docs/ARCHITECTURE.md), [Feature Map](docs/FEATURE-MAP.md),
   [Persistence](docs/PERSISTENCE.md), [Semester Sets](docs/SEMESTER-SETS.md).
4. [Project History](docs/PROJECT-HISTORY.md): 주요 milestone의 commit 및 검증 근거.
5. [Agent working rules](AGENTS.md): 자동화 도구의 작업 규칙.

특정 AI 제품, 대화 기록 또는 개인 메모는 작업 재개의 필수 자료가 아닙니다.
