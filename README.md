# School Timetable Widget Next

Modern Windows rewrite of School Timetable Widget.

## Status

- Product Contract v0.1 approved
- Phase 0 solution skeleton + development bootstrap
- Current Status Header, weekly timetable view and current-cell highlight implemented
- Subject/Class single-cell editing foundation complete in memory; automated tests and limited user native smoke passed; persistence not implemented
- Bulk School/Canonical timetable import, preview, atomic Apply and template copy implemented; automated tests and limited user native UX approved ([verification](docs/TIMETABLE-BULK-INPUT.md))

기술 baseline은 **WPF + .NET 10 LTS + CommunityToolkit.Mvvm**이다.
Desktop/Core/Tests 3개 project에서 표시·시간 계산·한 셀 편집 기반을 구현 중이다. 전체 제품 완성 단계는 아니다.

## Development

[Development Setup](docs/DEVELOPMENT.md): 신규 PC bootstrap, 환경 점검과 CLI build/test.

## Product direction

- 월~금 × 7교시 독립 35셀 시간표와 안전한 편집.
- Current Status Header에 `HH:mm:ss`와 학교 시간 상태를 함께 표시.
- KRISS 대한민국 표준시(KST) 우선 Application Clock, PC local time으로 즉시 시작 및 동기화 불가 시 fallback.
- DPI/multi-monitor에서 안정적인 크기·위치 유지.
- 안전한 persistence와 원본을 보존하는 Legacy migration.
- Tray, 선택적 autostart, 전체 profile backup/restore, PC 간 파일 공유.

QR, 초기 셀 병합, Legacy Python architecture reuse는 현재 제품 범위에서 제외한다.

## Documents

- [Product Contract v0.1](docs/PRODUCT-CONTRACT.md): authoritative 제품 계약.
- [Accepted ADRs](docs/adr/0001-golden-reference-policy.md): [Windows stack](docs/adr/0002-windows-desktop-stack.md), [Settings transaction](docs/adr/0003-settings-transaction.md), [Application time source](docs/adr/0004-application-time-source.md).
- [Legacy Reference](docs/LEGACY-REFERENCE.md): 고정된 Golden Reference와 증거 사용 범위.
- [Architecture](docs/ARCHITECTURE.md): 확정된 baseline, 구조 방향과 유보 결정.
- [Feature Map](docs/FEATURE-MAP.md): 기능별 계획 상태.
- [Agent rules](AGENTS.md): 장기 작업 규칙.
