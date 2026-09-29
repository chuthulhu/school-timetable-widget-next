# ADR 0024 — KRISS NTP synchronization for Application Clock

Status: **Accepted**

Date: 2026-09-29

Authority: explicit KRISS synchronization milestone request. This resolves ADR 0004's
network/correction/lifetime deferrals without rewriting its historical foundation evidence.

## Decision

- Official `ntp.kriss.re.kr:123` via .NET DNS/UDP, no UTCk redistribution or third-party NTP library.
- PC-local fallback first; primary window/tray startup never waits for DNS/network.
- One existing Application Clock snapshot contract, atomic UTC anchor/monotonic reference,
  explicit KST on success; no Windows clock changes, privileges or persisted correction.
- Three samples with at least two coherent valid samples; four timestamps, endpoint and
  packet validation, reference-based era unfolding. Large coherent offsets are permitted.
- Background 60-minute successful resync / 15-minute failure retry, single-flight and
  bounded cancellation, debounced Windows resume, hidden-tray continuation.
- Failed initial sync keeps PC fallback; later failure retains last-good reference with
  diagnostic age/failure. Restart discards it. Exit/session end does not wait for networking.
- Runtime diagnostics only; visible source/status UI from ADR 0004 remains deferred for
  this milestone. No new settings, notifications, profile/schema or recovery behavior.

## Alternatives and consequences

PC wall time plus an offset is simpler but repeats later manual Windows clock changes.
A full discipline/slew engine is disproportionate. A monotonic anchor provides continuity
between validated atomic corrections with a small testable implementation.
Sample consistency reduces accidental outliers, not malicious NTP spoofing or network asymmetry.
Clock corrections may change the school date; existing one-snapshot refresh resolves all
current-day facts together without changing the user's viewed week or selected semester.

Exact policy, source evidence and verification limits: [KRISS time synchronization](../KRISS-TIME-SYNC.md).
