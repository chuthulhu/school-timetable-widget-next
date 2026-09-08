# Legacy Golden Reference

| Reference | Value |
| --- | --- |
| Repository | `chuthulhu/school-timetable-widget` |
| Branch | `recovery-v1-release` |
| Golden Reference commit | `84de32a555633120bd6363a609a19cbc0a15e8ea` |
| Planning handoff commit | `a89d4ae8701788b744f89ee7b2407ba9df7ab488` |
| 성격 | Reconstructed & Characterized Legacy Golden Reference |

이 reference는 **exact deployed source recovery가 아니다**. 사용자 behavior, compatibility와
migration을 판단하는 evidence source이며 implementation template이 아니다.
Python architecture를 복사하지 않고 characterization bug를 새 contract로 자동 승격하지 않는다.
새 저장소는 Legacy와 독립된 Git history를 유지하며 Legacy source/history/.git을 복사하지 않는다.

## Fixed reference usage

동작·호환성 증거는 Golden Reference commit에서 읽는다. 승인된
[Product Contract](PRODUCT-CONTRACT.md)와 ADR 0001–0004의 이관 출처는 planning handoff commit이다.
두 commit의 역할을 구분하고, 재현 가능한 근거에는 이동 가능한 branch HEAD 대신 고정 commit을 사용한다.

Legacy local checkout: `D:\Onedrive\OneDrive - 진해여자고등학교\Codex\timetable\school-timetable-widget`.
로컬 경로는 개발자 환경에 따라 다를 수 있으며 저장소 정체성은 위 repository와 commit으로 확인한다.
로컬에서 특정 증거를 읽는 예:

```powershell
git -C "D:\Onedrive\OneDrive - 진해여자고등학교\Codex\timetable\school-timetable-widget" show "84de32a555633120bd6363a609a19cbc0a15e8ea:docs/TIMETABLE-BEHAVIOR-SPEC.md"
```

아래 증거 문서는 새 저장소에 복사하지 않는다. 필요할 때 Legacy의 고정 commit에서 읽는다.

| Legacy document | 용도 |
| --- | --- |
| [docs/LEGACY-RECOVERY-STATUS.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/LEGACY-RECOVERY-STATUS.md) | Recovery 완료 판정과 검증 범위 |
| [docs/RECOVERY-COMPLETION-AUDIT.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/RECOVERY-COMPLETION-AUDIT.md) | 기능별 evidence와 known quirks |
| [docs/FEATURE-MAP.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/FEATURE-MAP.md) | Legacy 데이터 의미와 소유권; 새 저장소의 tracking 문서와 구분 |
| [docs/TIMETABLE-BEHAVIOR-SPEC.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/TIMETABLE-BEHAVIOR-SPEC.md) | 시간표·교시·강조·편집 동작과 결함 |
| [docs/SETTINGS-BEHAVIOR-SPEC.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/SETTINGS-BEHAVIOR-SPEC.md) | Settings Preview/Apply/Cancel과 appearance 증거 |
| [docs/DATA-LIFECYCLE-SPEC.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/DATA-LIFECYCLE-SPEC.md) | Save/import/backup/restore 및 실패 경계 |
| [docs/WINDOWS-REFERENCE-RUNBOOK.md](https://github.com/chuthulhu/school-timetable-widget/blob/84de32a555633120bd6363a609a19cbc0a15e8ea/docs/WINDOWS-REFERENCE-RUNBOOK.md) | Windows native 검증 조건, lifecycle 및 미검증 범위 |

## Porting principles

- **MATCH:** 정상 사용자 의미를 유지한다. Qt API, pixel 값 또는 내부 구조를 복제하는 요구가 아니다.
- **COMPATIBLE:** 기존 입력 의미를 읽어 새 schema로 변환한다. Legacy schema를 내부 구조로 고정하지 않는다.
- **REDESIGN:** 데이터 손실, 불명확한 저장, lifecycle quirks를 분리하고 승인된 새 계약을 따른다.

이 분류는 승인 상태가 아니다. 제품 결정과 DEFERRED 경계는 Product Contract와
[ADR 0001](adr/0001-golden-reference-policy.md)을 따른다.
Source, automated, Qt object/event, native 증거를 구분하며 Legacy 검증을 .NET 검증으로 간주하지 않는다.
