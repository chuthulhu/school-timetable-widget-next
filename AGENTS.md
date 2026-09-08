# Agent Working Rules

1. [Product Contract](docs/PRODUCT-CONTRACT.md)와 Accepted ADR이 구현보다 우선한다. ADR은 [0001](docs/adr/0001-golden-reference-policy.md), [0002](docs/adr/0002-windows-desktop-stack.md), [0003](docs/adr/0003-settings-transaction.md), [0004](docs/adr/0004-application-time-source.md) 및 후속 Accepted 결정을 따른다.
2. [Legacy Golden Reference](docs/LEGACY-REFERENCE.md)는 behavior/evidence source이며 implementation template이 아니다.
3. Characterization test가 존재한다는 이유로 Legacy quirk를 복제하거나 새 test expectation으로 채택하지 않는다.
4. 사용자에게 보이는 contract를 변경할 때 관련 문서와 필요한 ADR을 함께 갱신한다. 문서/ADR 없이 계약을 바꾸지 않는다.
5. 기능·UX·data·architecture 의미가 애매하면 구현 전에 사용자 의도를 확인한다.
6. 사용자 경험 목표가 numeric example이나 test fixture보다 우선한다. 예시 수치를 의도와 무관한 계약으로 확대하지 않는다.
7. 거대한 MainWindowViewModel에 기능 책임을 집중하지 않는다.
8. 거대한 AppState에 모든 상태와 동작을 집중하지 않는다.
9. Shared/Utils를 관련 없는 책임의 dumping ground로 사용하지 않는다.
10. Windows-specific 구현은 명시적 Windows boundary에 격리한다.
11. Persistence, committed state, Preview runtime state를 구분하고 소유권과 성공 경계를 명확히 한다.
12. 모든 시간 기능은 하나의 공통 Application Clock을 사용한다.
13. 기능 코드에서 `DateTime.Now` / `DateTimeOffset.Now`를 직접 사용하지 않는다. PC 시간 읽기는 clock의 fallback 경계에 둔다.
14. Migration은 Legacy source data를 수정·이동·삭제하지 않는다.
15. Save/import/restore는 partial committed state를 남기지 않는다.
16. 실패를 성공처럼 표시하지 않는다. 저장된 선호와 실제 OS 적용 상태를 구분한다.
17. 작업 전에 관련 contract, ADR, reference를 읽고 승인 상태와 증거 범위를 확인한다.
18. 변경 시 관련 test/docs/verification을 함께 갱신한다. 실행하지 않은 검증을 완료했다고 주장하지 않는다.
19. 큰 cross-feature 변경보다 작고 명확한 feature boundary 변경을 선호한다.
20. 구현 세부를 Legacy source에서 무비판적으로 복사하지 않는다. Legacy source/history/.git을 이 저장소로 복사하지 않는다.

## Windows verification

- 사용자가 다른 앱에서 작업할 수 있도록 source inspection, isolated TEMP profiles, contract tests, process-local time injection, application-object/event tests를 우선한다.
- Background 검증 중에는 창 활성화, 포인터 이동, 키 입력, clipboard 변경, foreground dialog를 사용하지 않는다. 최소화나 다른 Windows virtual desktop을 input 격리로 간주하지 않는다.
- Native input이 필요하면 필요한 foreground 구간을 설명하고 사용자와 조율한다. 이미 승인된 구간에서는 일상적인 동작마다 반복 승인 요청을 하지 않는다.
- Object/event test는 실제 keyboard, 한글 IME, Tab/focus-loss, title-bar 및 OS input 증거가 아니다. 증거 방법과 변경된 window flags/hidden/offscreen 조건을 정확히 기록한다.
- 진단은 고유 TEMP profile과 소유 process로 격리하고 원본 설정을 보호한다. 시스템 clock을 변경하지 않으며 소유한 진단 process만 종료한다.
