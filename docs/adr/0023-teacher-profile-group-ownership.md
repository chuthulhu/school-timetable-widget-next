# ADR 0023 — Teacher profile/group future ownership boundary

Status: **Accepted — foundation only; implementation deferred**. Date: 2026-09-29.
Authority: explicit Teacher Profile / Group Foundation-only milestone request.

## Context

ADR 0022 implements Semester Sets within the current implicit single-teacher product.
Future teacher profiles/groups need stable identities and a logical ownership boundary
without prematurely changing that valid implementation or choosing a new storage layout.

## Decision

- TeacherTimetableProfile has stable opaque ProfileId and DisplayName. Rename retains
  ProfileId; DisplayName is not identity.
- TimetableGroup has stable opaque GroupId, DisplayName and ProfileId references.
  Membership may be many-to-many. Groups do not copy timetable data; profile rename keeps
  membership, and group deletion does not delete profiles or their timetable data.
- Teacher-specific timetable ownership has logical key **(ProfileId, SemesterId)**,
  covering the 35-cell WeeklyTimetable and teacher-specific date timetable overrides.
  SemesterId remains the stable semester identity.
- Base PeriodSchedule and date-specific schedule overrides have school/semester meaning;
  avoid unnecessarily duplicating them per teacher. The future architecture can separate
  these inputs from teacher-specific timetable ownership.
- Current SemesterSet ownership, ActiveSemester semantics, single-teacher behavior,
  profile schema v5 and backup envelope v1 are unchanged. No fake default-teacher UI.
- Physical persistence nesting, future schema/compatibility, SelectedProfileId storage,
  selector hierarchy/UX and per-profile versus global active-semester selection are deferred.

## Consequences and limits

The [foundation contract](../TEACHER-PROFILE-GROUP-FOUNDATION.md) records future import,
lossless migration into one implicit/default Teacher Profile across all existing semesters,
and full-backup inclusion of all profiles/groups/semester-specific teacher timetables.
Those are future compatibility requirements, not implemented migrations or formats.

This settles only the formerly deferred logical teacher/semester relationship. It does not
supersede ADR 0022's current physical implementation, require a refactor, or approve
multi-teacher feature implementation. Do not create speculative types, services or UI.
Teacher Profiles / Groups remain **DEFERRED — FOUNDATION READY** after this milestone.
