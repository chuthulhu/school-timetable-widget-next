# ADR 0011 — Effective day and date overrides

Status: **Accepted**. Date: 2026-09-10.
Authority: explicit milestone request and subsequent user confirmation.

A date timetable override is a complete immutable seven-cell snapshot, ordered
by period 1–7, for one weekday DateOnly. It is not a 35-cell week or a sparse patch.
Creation copies that weekday's base values into a Draft. Resolution projects those
seven values into the otherwise unchanged base week. Period schedule overrides are
independent complete chronological schedules with the existing seven-period invariant.
Weekends remain authoritative noninstructional days; creation on weekends is rejected.

One clock snapshot supplies the date to one pure effective-day resolution per refresh.
Status, countdown, date/time/header, highlight and grid consume its captured results.
The default-OFF lunch option follows ADR 0010 and the effective period 4/5 endpoints.

Cell editing follows displayed provenance, never text equality. Schedule-only
overrides leave timetable provenance at Base. At opening, source, optional date,
weekday and period are fixed; midnight never retargets an open editor. Target labels
identify Base or the date. Stale replaced/removed target snapshots reject before
mutation. Bulk import remains explicitly Base-only; date import stays planned.

Runtime stores own private DateOnly maps. Complete timetable/schedule candidates
validate before one logical date replacement; disabled components fall back to Base,
and both disabled remove the entry. Cancel/X/Escape discard Draft. No persistence,
profiles, semester sets or Settings framework. Timetable snapshot values can later
be owned by teacher profiles independently of school-day schedules.

Date selection precedes editing in the date dialog. Starting an edit fixes its date;
returning to date selection explicitly discards that unapplied Draft. Successful
Apply closes. This avoids silently assigning an existing Draft to a new date.

Implementation and verification status: see ../DATE-OVERRIDES.md.
