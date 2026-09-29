# Teacher Profile / Group Foundation

Status: **FOUNDATION DOCUMENTED / IMPLEMENTATION DEFERRED**. Approved 2026-09-29
by the explicit foundation-only milestone. Authority: [Product Contract](PRODUCT-CONTRACT.md)
and [ADR 0023](adr/0023-teacher-profile-group-ownership.md).

## Purpose and current product

Record the minimum future identity and logical ownership boundaries so that later teacher
timetables and groups can extend Semester Sets, persistence and import deliberately.
This milestone does not authorize or implement the multi-teacher feature.

Today the product has one **implicit single teacher/profile**, with multiple Semester Sets.
There is no teacher identity field, teacher selector or artificial “기본 교사” profile UI.
The portable `profile.json` / `ProfileSnapshot` aggregate is not a TeacherTimetableProfile:
it currently contains all semesters and global settings. Profile schema v5, `.stwbackup`
envelope v1 and every current user-visible workflow remain unchanged.

## Future identities and group references

| Concept | Minimum meaning |
| --- | --- |
| TeacherTimetableProfile | Stable opaque ProfileId and DisplayName. DisplayName is a label, never identity; rename retains ProfileId. |
| TimetableGroup | Stable opaque GroupId, DisplayName and references to ProfileIds. DisplayName is independent of GroupId. |
| SemesterSet | SemesterId remains the stable semester identity established by ADR 0022. |

A group such as `3학년 담임` or `과학교사` references profiles; it does not deep-copy
their timetables. Membership permits many profiles per group and many groups per profile.
Renaming a profile preserves its memberships through ProfileId references. Deleting a
group does not delete its referenced Teacher Profiles or their actual timetable data.
ID encoding/generation and profile/group naming validation are future implementation decisions.

## Logical timetable ownership and school schedules

The future logical key for teacher-specific timetable data is **(ProfileId, SemesterId)**.
Each such association can hold a complete 35-cell WeeklyTimetable and teacher-specific
date timetable overrides. A date override is scoped to that teacher/semester association;
equal dates in other associations do not identify the same teacher timetable data.
This boundary does not change existing independent-cell or complete-day override semantics.

Base PeriodSchedule and date-specific PeriodSchedule Overrides have school/semester-level
schedule meaning. The architecture should share that meaning rather than unnecessarily
duplicate schedules for every teacher. Teacher class content and school-day timing remain
separate concerns. This does not introduce a School entity, schedule registry or teacher
schedule exceptions, nor decide their physical representation.

Current v5 SemesterSet legitimately contains both timetable and schedule, including both
independent date override components. That is a valid single-teacher implementation, not
technical debt requiring immediate refactoring. ADR 0022's current ownership and
ActiveSemester behavior remain in force. A future multi-teacher implementation may separate
teacher/semester timetable inputs from school/semester schedule inputs while preserving
their meaning and resolving them with the shared Application Clock snapshot.

## Selection and physical design remain deferred

Future multi-teacher selection will likely need conceptual SelectedProfileId together with
ActiveSemesterId. This does not add a persisted selection field today or decide:

- Which selector is primary, or MainWindow selection/group/tab UX.
- Whether active semester is remembered per profile or is one global active SemesterId.
- Profile → Semester versus Semester → Profile JSON nesting, tables, repositories or types.
- Future schema version, compatibility rules, migration mechanics or profile deletion UX.

The actual multi-teacher implementation milestone must settle those Product Contract
decisions. This foundation authorizes none of them implicitly.

## Future import, migration and backup boundaries

A future school-wide spreadsheet import must be able to create/update several
TeacherTimetableProfile candidates and update each profile's timetable for the relevant
SemesterId. Explicit identity mapping must distinguish creation from updating an existing
ProfileId; a candidate label or teacher name is not an identity. Mapping/preview/apply UX
and physical import design remain deferred. The current School/Canonical parser and UI
continue applying one selected candidate to the captured active semester's Base week only.

Future migration must be able to assign **all existing teacher-specific timetable data
across every semester**, including date timetable overrides, losslessly to one implicit/default
Teacher Profile. Semester identities, school schedules and other existing inputs must remain
preserved. “Default” describes migration ownership, not a new current user-facing name.
No migration code or schema v6 is introduced now. Existing all-or-nothing save/import/restore,
corrupt-original preservation and immutable Legacy-source contracts still apply.

Once multi-teacher support is actually implemented, Full Profile Backup must include all
profiles, groups and semester-specific teacher timetable data, alongside the other portable
profile inputs. Schema/backup compatibility is decided in that future migration milestone.
Today `.stwbackup` stays v1 with embedded v5 exports and existing v4/v5 reads; no version bump,
teacher-specific backup format or recovery change occurs here.

## Explicit non-goals

No schema/JSON shape change, migration implementation, Teacher Profile/Group management UI,
teacher selector or selected-teacher persistence, multi-profile import, backup-format change,
Semester Sets behavior change, speculative unused types/interfaces/services/repositories,
or preparatory Semester refactor. Current single-teacher workflows stay identical.
After documenting this foundation, Teacher Profiles / Groups return to **DEFERRED** so the
next product milestone can proceed independently.

## Source alignment and verification scope

Read against the current implementation at ef6c977 (unchanged at the starting handoff
96081ff): Core `Features/Semesters/SemesterSet.cs`; Desktop
`Features/Persistence/ProfileSnapshot.cs`, `Infrastructure/Persistence/ProfileJson.cs`,
`Features/Semesters/SemesterManagement.cs`, `Features/Persistence/ProfileBackupFile.cs`;
and Core `Features/TimetableImport/TimetableImportCandidate.cs`.
They confirm current semester ownership, v5 serialization, unchanged backup envelope and
display-only import labels. This document adds no production code or test expectations.
Automated regression evidence is recorded in [Continuity](CONTINUITY.md); it is not native
multi-teacher evidence or an implementation claim.
