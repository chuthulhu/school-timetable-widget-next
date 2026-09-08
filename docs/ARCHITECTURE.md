# Architecture

현재는 documentation bootstrap 단계이며 production skeleton은 없다.
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

검토할 의존 방향 후보:

```text
UI/ViewModel
  → feature use case/interface
  → infrastructure/platform implementations
```

이는 역할 관계를 논의하기 위한 후보이며 실제 assembly reference graph나 구현체의 직접 의존을
확정한 것이 아니다. 인터페이스 소유권, 조립 방식과 실제 project/assembly/folder layout은 Phase 0에서 결정한다.

## Deferred decisions

| 항목 | 상태 |
| --- | --- |
| Project 분할 수, assembly/folder layout | DEFERRED — Phase 0 |
| Exact namespace 및 type 배치 | DEFERRED — Phase 0 |
| DI container와 composition 방식 | DEFERRED |
| Persistence technology/schema, concurrency 및 crash recovery | DEFERRED |
| Installer/updater technology와 배포 상세 | DEFERRED |
| Windows App SDK 사용 범위 | DEFERRED |

현재 문서는 skeleton, 구현 완료 또는 native 검증 완료를 의미하지 않는다.
Clock endpoint/client/보정 구현 등 feature별 유보 사항은 Product Contract와 해당 ADR을 따른다.
