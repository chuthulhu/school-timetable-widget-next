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

## Canonical handoff

Start at [CONTINUITY](docs/CONTINUITY.md), then the Product Contract and
[Accepted ADR index](docs/adr/README.md). Record durable state/decisions in canonical docs;
keep this file for working rules. Chat history or tool memory must not be required.
Dated native checkpoints are evidence of their original scope, not new approval gates.

## Windows verification

Follow [Development verification policy](docs/DEVELOPMENT.md#non-interfering-windows-verification).
Prefer non-interfering source/contract/object tests and isolated TEMP data while the user works.
Never describe activation, keyboard/pointer injection, clipboard changes or foreground dialogs
as background checks. Coordinate native input only when needed and not already authorized.
Report evidence methods and changed test conditions precisely; do not generalize a historical
tool discovery/visibility failure. Protect production data and terminate only owned diagnostics.

## Cloud development

Follow [Cloud development](docs/CLOUD-DEVELOPMENT.md) for Linux setup and the
Windows CI gate. Linux setup builds Core only; the existing full test project
requires Windows. Do not retarget the app or remove tests to make Linux pass.
Report Core/cross-build evidence separately from Windows test/native evidence.
Keep real profiles, backups, recovery evidence and credentials out of Git.
