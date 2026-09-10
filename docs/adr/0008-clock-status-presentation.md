# ADR 0008 — Current date and replaceable clock/status presentation

Status: **Accepted**

Date: 2026-09-10

Decision status: **APPROVED — 사용자 CurrentDateText 구현 및 future customizable
clock/status presentation 요구**. Settings 기능의 즉시 구현 승인은 아니다.

## Decision

- Desktop formatter/result/ViewModel은 `CurrentDateText`, `CurrentTimeText`,
  `StatusText`를 독립적으로 제공한다. 현재 기본값은 invariant Gregorian
  `yyyy년 MM월 dd일`, invariant 24시간 `HH:mm:ss`, 기존 A8 한국어 상태 문구다.
- 기존 refresh cycle이 읽은 하나의 `ApplicationTimeSnapshot`에서 date/time,
  status/countdown 및 highlight를 모두 파생한다. 추가 clock read/timer는 없다.
  미래 AM/PM, weekday, 12시간 표시도 동일 snapshot에서 파생한다.
- Core는 snapshot/status/countdown 의미를 소유한다. Format/font/size/layout/preset과
  한국어 AM/PM 문자열은 Desktop 책임이다. Core에 표시 설정/WPF 타입을 넣지 않는다.
- 현재 별도 UserControl의 한 줄 date/time/status는 standard native candidate다.
  고정 높이와 date/time slot은 ordinary tick의 위치·높이 안정성을 위한 현재 구현이다.
  한 줄 구조, slot 폭, font 크기를 영구 제품 계약으로 만들지 않는다.
- Future Standard/Large Digital/Compact/Minimal은 교체 가능한 presentation 방향이며
  정확한 이름/배치/Settings schema는 후속 UX에서 결정한다. Explicit preset/setting
  변경에 따른 preferred size 및 re-layout은 허용하되 normal tick은 geometry를 바꾸지 않는다.
- 필요한 세 값만 구현한다. Preset enum/provider hierarchy, AmPmText/WeekdayText,
  Settings object/schema/UI/persistence 및 third-party font를 선제 도입하지 않는다.

## Consequences

현재 presentation을 교체해도 Core 시간 계산이나 clock source를 바꿀 이유가 없다.
추후 preset 기본값 + 사용자별 font size/weight/alignment/spacing override를 고려한다.
Date/time/status font size는 각각 조절할 수 있어야 한다. Font 선택은 기본 system font와
향후 digital/seven-segment 후보를 구분하며, bundled font 도입 시 배포·상업적 사용
license를 확인한다. 이번에는 font 파일이나 dependency를 추가하지 않는다.

A4/A8의 시각·상태 항상 표시 및 24시간 기본은 현재 candidate에 적용한다.
향후 show/hide/12시간 옵션은 [Product Contract](../PRODUCT-CONTRACT.md)의 후속 요구다.
예전 milestone의 날짜 표시 제외 기록은 이번 명시적 구현 요청을 금지하지 않는다.
Period Schedule Editing Foundation을 full display Settings 구현으로 확대하지 않는다.

검증 및 future 항목은 [Architecture](../ARCHITECTURE.md#current-date-and-future-clockstatus-presentation--2026-09-10)와
[Feature Map](../FEATURE-MAP.md#clockstatus-presentation--2026-09-10)에 기록한다.

## Follow-up — per-element font sources, 2026-09-10

사용자가 Bundled/System/Online source와 optional Local Font File 방향을 추가했다.
Title/Time/AmPm/Date/Weekday/Status가 각각 다른 source/family를 선택할 수 있어야 한다.
CurrentDateText/CurrentTimeText/StatusText 및 독립 TextBlock은 그대로 유지한다.
Header 공통 FontFamily를 영구 계약으로 만들거나 font 설정 타입을 미리 도입하지 않는다.

Online은 사용자 요청 → approved HTTPS provider → download → validate/app-local cache
→ local/private FontFamily resolution이다. UI의 remote font URL 및 startup/clock tick의
network 의존은 금지한다. Missing/offline/download 실패는 safe fallback으로 처리한다.
Canonical 설정은 stable source/family/provider identity이며 machine absolute path나
font binary에 종속되지 않는다. Format 지원 spike 및 license/metadata 검증은 실제
Font milestone에서 수행한다. 정확한 source/cache/schema 구현은 현재 범위 밖이다.
상세 future 요구는 ARCHITECTURE/FEATURE-MAP을 따른다.
