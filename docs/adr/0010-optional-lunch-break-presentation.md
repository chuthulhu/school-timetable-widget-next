# ADR 0010 — Optional lunch label for Break presentation

Status: **Accepted**
Date: 2026-09-10
Authority: user's explicit future product requirement.
Implementation: **PLANNED — NOT IMPLEMENTED**.

## Decision

Keep exactly the existing five Core CurrentStatusKind values. Do not add Lunch or
change status/countdown/highlight semantics. A default-OFF Desktop presentation
option selects the lunch label only when the already resolved status is Break and
Period 4 End <= local current time < Period 5 Start in the same day's effective
schedule. Use period identity, not hard-coded times, gap length or list position.
Touching periods have an empty interval and never display lunch.

OFF preserves every existing Break string. ON within that interval displays
`점심시간 · 5교시까지 {Countdown}`; other Breaks retain the ordinary label. Other
status kinds are unchanged. Countdown still comes from the existing Core calculation
up to period 5 Start. A lunch label never creates a current-cell highlight.

## Minimal planned boundary

The next Effective Day / Date Override design resolves one effective configuration
from the cycle's one ApplicationTimeSnapshot. It captures the effective period
schedule once and supplies that exact immutable value to status/countdown/highlight
and Desktop presentation. Base edits and date-specific overrides therefore affect
the label through the same effective snapshot, without another source read.

Extend the Desktop formatter's inputs with that effective schedule and a boolean
presentation option (conceptual name `showLunchBetweenPeriods4And5`, default false).
A pure Break-label selection inside the formatter checks status, snapshot time and
period 4/5 endpoints. It uses the existing countdown presentation unchanged. The
formatter does not query clock/schedule sources, resolve effective configuration,
change domain status or own Settings. No separate lunch timer/resolver/service,
Core display option, generic policy hierarchy or new previous-period domain field
is necessary for this presentation rule.

The composition layer supplies the option. A future Settings checkbox may own the
preference; UI, persistence and exact public API are not implemented by this decision.
Until implemented, all runtime Break text remains the default ordinary label.

## Planned verification, not executed

OFF: every Break keeps existing text, including a long period-4-to-5 gap.
ON: exact period 4 End shows lunch when Break; just before period 5 Start still
shows lunch; exact period 5 Start is InPeriod; touching has no lunch interval.
Other gaps and all other status kinds are unchanged. Move endpoints through base
editing and a date override and verify the label follows the effective identity
rather than original times. A switching fake clock/source must still be read once
per cycle; date/time/status/countdown/highlight and label share those captured inputs.
Verify existing countdown semantics and no current-cell highlight during lunch.
