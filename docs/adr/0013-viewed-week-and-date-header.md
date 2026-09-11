# ADR 0013 — Viewed week and date header

Status: **Accepted**. Date: 2026-09-11.
Authority: explicit Week Navigation + Date Header milestone request; the prior future
requirement in Architecture/Feature Map supplies the existing semantics.

## Decision

ViewedWeekStart is Monday-based Desktop timetable UI state, initialized from the
first shared Application Clock snapshot before showing MainWindow. Saturday/Sunday
start with their containing Monday. Previous/Next move exactly seven days. No tick,
midnight, Monday boundary or source correction automatically changes the viewed week.
At DateOnly bounds, a move that cannot produce all five dates is disabled.

WeeklyTimetableViewModel owns browsing alongside its stable 35 cells, with navigation
in a dedicated partial file. Five TimetableDateColumn objects carry DateOnly, SchoolDay,
invariant M/d text, Korean weekday text, IsToday, seven cells and their typed date
component provenance. Complete seven-cell overrides resolve independently by exact date;
schedule-only overrides keep Base timetable provenance. The committed Base week remains
separate. No new global state, feature assembly or clock is introduced.

CurrentStatusRefreshLoop still reads one actual snapshot and resolves one actual effective
schedule per cycle for date/time/status/countdown/lunch. It passes actual date/current
slot facts to the timetable. Date/weekday headers and highlighted cells belong to the
viewed week; current highlight is visible only when that exact actual date is displayed.
Today indication changes background only. Ticks preserve column and cell objects.

Navigation and relevant successful data changes refresh the five columns. ProfileRuntime
wires the date map and refreshes a displayed date after durable date Apply/removal,
independently of whether that date is today. Base edits/import reproject all displayed
sources after save, preserving every date override. Editors capture source/date/slot at
open, retaining stale rejection and existing explicit Base/date target labels.

The two-row date/weekday header and flanking arrows retain five equal aligned body
columns. Button spacing/colors remain native candidates. Date click/calendar/Today
button remain deferred. Viewed week is never saved; schema 1 and its durable inputs
are unchanged. Restart returns to the actual current week.

## Superseded scope

ADR 0011's single-today grid projection describes the earlier milestone. This accepted
follow-up replaces that grid-only restriction and its refresh coupling; the actual-day
status pipeline, immutable day values, editing and independent schedule rules remain.
ADR 0012's persistence boundary is unchanged. See [Week Navigation](../WEEK-NAVIGATION.md)
for implementation status and evidence; acceptance here is product/design authority,
not a claim of native UX approval.
