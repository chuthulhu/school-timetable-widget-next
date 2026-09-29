# Feature Map

Current implementation at ef6c977 (Semester Sets), reviewed 2026-09-29.
[Product Contract](PRODUCT-CONTRACT.md) and [Accepted ADRs](adr/README.md) define the
requirements; this page tracks delivery, not approval. Historical progression is in
[Project History](PROJECT-HISTORY.md) and dated feature verification records.

## Implemented

| Feature | Current boundary / evidence |
| --- | --- |
| Windows WPF solution and CLI bootstrap | Three projects; [Development](DEVELOPMENT.md) |
| Mon–Fri × seven independent cells | Lossless Subject/Class, plain text, editing and layout-stable highlight; [Editing](TIMETABLE-EDITING-FOUNDATION.md) |
| School/Canonical bulk import and template copy | Strict quoted TSV, candidate/mapping preview, captured semester Base-only Apply; [Bulk](TIMETABLE-BULK-INPUT.md) |
| Base period editing | Complete chronological seven-period validation and durable Apply; [Periods](PERIOD-SCHEDULE-EDITING.md), [Persistence](PERSISTENCE.md) |
| Current Status Header/countdown/date/highlight | Shared snapshot, five Core states, exact [start,end), no tick-driven geometry changes; [Architecture](ARCHITECTURE.md) |
| Application Clock foundation | Interface/snapshot, injected tests and PC fallback implemented; KRISS network sync remains below |
| Date timetable/period overrides and lunch | Independent complete components per semester/date; global default-OFF lunch presentation; [Overrides](DATE-OVERRIDES.md) |
| Week navigation / date columns / Today indicator | Exactly ±7 days, per-date provenance, browsing independent of actual status, no viewed-week persistence; [Navigation](WEEK-NAVIGATION.md) |
| Profile persistence / degraded handling | Current writer v5, strict v1–v5 readers, preserve corrupt originals and block writes; [Persistence](PERSISTENCE.md) |
| Display presets and settings transaction | Four defaults, Time/Date/Weekday/Status typography, format/visibility, live Preview and last-Apply rollback; [Display](DISPLAY-SETTINGS.md) |
| User display presets | Stable IDs, Save As/select/rename/update/inactive delete, shared display/library transaction; [Display](DISPLAY-SETTINGS.md) |
| Bundled/system/online fonts | Licensed resources, installed families, explicit pinned download/local cache and fallback; [Fonts](FONT-CATALOG.md) |
| One-preset file sharing | .stwpreset v1, strict preview/collision decision, Draft until Apply; [Preset files](PRESET-IMPORT-EXPORT.md) |
| Full profile backup/restore and Recovery Required | .stwbackup v1, all semesters/global data, full rollback, explicit recovery; [Backup](BACKUP-RESTORE.md) |
| Multiline screen fit | Content growth/shrink, monitor cap and body-only vertical scrolling; [Backup screen-fit evidence](BACKUP-RESTORE.md) |
| Window placement/preferred size | Machine-local state, completed gestures/reset, current monitor/DPI fit; [Placement](WINDOW-PLACEMENT.md) |
| Tray / single instance | X hides, explicit Exit, hidden refresh, secondary activation; [Lifecycle](TRAY-LIFECYCLE.md) |
| Windows autostart | Explicit tray toggle, exact per-user OS registration/read-back; [Autostart](AUTOSTART.md) |
| Semester Sets | Ordered stable identities, explicit switch/create/copy/rename/inactive delete, scoped edits, v5 storage/all-semester restore; [Semesters](SEMESTER-SETS.md) |

Automated baseline: 1,083 tests; native approvals cover only scenarios documented in the
linked evidence. “Implemented” does not imply release readiness or all native paths verified.
Profile schema v1–v4 in older milestone records describes prior writers, not the current writer.

## Remaining work

| Feature | Status / remaining boundary |
| --- | --- |
| Teacher profiles/groups | DEFERRED — FOUNDATION READY / NOT IMPLEMENTED; stable IDs, many-to-many references and (ProfileId, SemesterId) logical ownership; [Foundation](TEACHER-PROFILE-GROUP-FOUNDATION.md), [ADR 0023](adr/0023-teacher-profile-group-ownership.md) |
| Multi-teacher import | DEFERRED; future explicit creation/update mapping of several profiles and their relevant SemesterId timetables; current single-candidate Base import unchanged |
| KRISS synchronization | PLANNED; endpoint/client/correction/retry/resync and sync UX deferred; PC fallback is implemented |
| Suspend/resume integration | DEFERRED; OS detection and sync/notification behavior not implemented |
| Class notifications | PLANNED; scheduling/dedup/resume and actual Windows delivery; first-close tray notice is a different feature |
| Legacy migration / legacy backup import | PLANNED; immutable source, preview and all-or-nothing conversion; native v1–v4 migration is already implemented |
| Selective timetable/period file sharing | PLANNED under P10; distinct from implemented preset files and full-profile backups |
| Installer / updater | DEFERRED; technology, distribution/signing/rollback, uninstall data policy and supported OS matrix |
| Move lock / final z-order | DEFERRED; placement/tray do not implement move locking |
| Colors/themes/opacity and final visual design | PLANNED; existing typography/display settings are implemented, full theme/highlight editor is not |
| Title / independent AM-PM typography, additional date formats/alignment/spacing | FUTURE; current AM-PM uses Time typography; implemented format/visibility subset is listed above |
| Arbitrary local font import / cache management UI | OPTIONAL FUTURE; curated bundled/online catalog is implemented |
| Small rectangular paste / date-target bulk import / .xlsx template export | FUTURE; current School/Canonical Base import and TSV template copy are implemented |
| Today button / calendar/date-click navigation / remember viewed week | DEFERRED candidates; current previous/next week and restart-current-week behavior are implemented |
| Upcoming highlight | OPTIONAL / DEFERRED; current highlight is implemented |
| Semester ranges/auto-selection/reorder/history and special school calendar | NOT IMPLEMENTED; current explicit Semester Sets do not infer dates from names |

No new implementation or schema change is authorized by this inventory.
[Continuity future boundary](CONTINUITY.md#future-teacher-profiles-and-groups--deferred-decision)
records the approved logical ownership; physical schema and selection design remain deferred.
Semester Sets remain COMPLETED; the teacher/group foundation adds no implemented feature.

## Clock/Status presentation — 2026-09-10

Compatibility anchor for the original ADR 0008 link. That dated requirement is now implemented
for the subset listed above (ADR 0014–0017); only the explicitly remaining customization
items in the roadmap are future work. Detailed historical evidence remains in
[Display Settings](DISPLAY-SETTINGS.md) and [Font Catalog](FONT-CATALOG.md).
