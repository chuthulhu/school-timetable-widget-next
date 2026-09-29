# Project history

Major milestones reconstructed from Git commits and checked-in verification documents.
This is a result/decision index, not a chat transcript. Dates and “pending” descriptions inside
old records belong to their milestone; final native acceptance and later commits supersede them.
Use `git show <sha>` to inspect the exact code/doc change. Short SHAs below resolve uniquely
in this repository. No historical test count is inferred.

| Milestone / result | Commit(s) | Format impact | Verification / decision evidence |
| --- | --- | --- | --- |
| Approved product and immutable Golden Reference policy | a1008e5 | None | [Contract](PRODUCT-CONTRACT.md), [ADR 0001](adr/0001-golden-reference-policy.md) |
| .NET 10/WPF three-project bootstrap | b7337ed | None | [Development](DEVELOPMENT.md), [ADR 0005](adr/0005-phase-zero-project-structure.md) |
| Application Clock, period/status/countdown and presentation pipeline | 5cb8577, 676353b, 8fba84f, 9e31ecf, 2469c43, ef2f7ee | Runtime only | [Architecture phases 0.2–0.7](ARCHITECTURE.md), [ADR 0004](adr/0004-application-time-source.md) |
| Live Current Status Header | 07a87f7 | None | [Architecture phase 0.8](ARCHITECTURE.md#phase-08-header-view-and-app-wiring--user-native-smoke-passed) |
| Weekly timetable and current-cell highlight | 22c66c3, fa0b492 | None | [Architecture](ARCHITECTURE.md) |
| Subject/Class editing | 9cae616 | In-memory at this milestone | [Editing](TIMETABLE-EDITING-FOUNDATION.md), [ADR 0006](adr/0006-single-cell-in-memory-editing.md) |
| School/Canonical bulk input and preview | 17367af | TSV input, no profile writer yet | [Bulk](TIMETABLE-BULK-INPUT.md), [ADR 0007](adr/0007-bulk-timetable-input.md) |
| Editable base periods and current date | 6c6b5db | In-memory at this milestone | [Periods](PERIOD-SCHEDULE-EDITING.md), ADR 0008/0009 |
| Independent date overrides/effective day and optional lunch | d5fc840 | In-memory at this milestone | [Overrides](DATE-OVERRIDES.md), ADR 0010/0011 |
| Native persistence and corrupt-load preservation | 78c4d7d | Profile v1 | [Persistence](PERSISTENCE.md), ADR 0012 |
| Week browsing and per-date columns | 45418fe | Viewed week stays runtime-only | [Navigation](WEEK-NAVIGATION.md), ADR 0013 |
| Display settings/presets | 8cdcf64 | Profile v2, strict v1 reader | [Display](DISPLAY-SETTINGS.md), ADR 0014 |
| User display preset library and duplicate-window protection | fbb780e, 26f6614 | Profile v3, v1/v2 readers | [Display](DISPLAY-SETTINGS.md), ADR 0015 and ADR 0003 follow-up |
| Licensed bundled/online font catalog and preset selector correction | 0e50c5d | Profile v4, strict v1–v3 readers | [Fonts](FONT-CATALOG.md), ADR 0016 |
| One-preset sharing | 10c3413 | .stwpreset v1, profile unchanged | [Preset files](PRESET-IMPORT-EXPORT.md), ADR 0017 |
| Full backup/restore, Recovery Required, multiline screen-fit correction | acf911c | .stwbackup v1 embeds profile v4 at introduction | [Backup](BACKUP-RESTORE.md), ADR 0018 |
| Preferred window placement/size | 77bf2e1 | Separate machine-local window-state v1 | [Placement](WINDOW-PLACEMENT.md), ADR 0019 |
| Close-to-tray / explicit Exit / single instance | b5076ed | Separate machine-local tray-state v1 | [Lifecycle](TRAY-LIFECYCLE.md), ADR 0020 |
| Windows autostart | d36dc98 | OS registration only | [Autostart](AUTOSTART.md), ADR 0021 |
| Semester Sets and all-semester recovery | ef6c977 | Profile v5; backup envelope stays v1, new embedded schema v5 | [Semesters](SEMESTER-SETS.md), ADR 0022; final UX approval 2026-09-29 |

The baseline commit is ef6c977abbfdd9725183614701bb90bc3c03a5a8.
The continuity milestone updates documentation and one obsolete bootstrap status message only;
application source, test expectations, dependencies and schema are unchanged.
Its cleanup and clean-clone evidence are recorded in [Continuity verification](CONTINUITY-VERIFICATION.md).
