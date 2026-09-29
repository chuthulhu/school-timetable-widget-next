# KRISS Application Clock Implementation Plan

> Execution: inline using executing-plans and TDD; final independent review.

**Goal:** Implement the explicitly requested KRISS clock without blocking startup or changing Windows time.
**Architecture:** Existing Core snapshot API; small Desktop Time infrastructure boundaries; Windows resume adapter.
**Tech stack:** Existing .NET 10/WPF; built-in TimeProvider, DNS, UDP.
**Spec:** [KRISS design](../../KRISS-TIME-SYNC.md) and [ADR 0024](../../adr/0024-kriss-application-clock-sync.md).

## Constraints and rulings

Work on the user-specified clean main checkout. No intermediate commits: user native UX
acceptance is required before commit, then ordinary fast-forward app push, followed by registry refresh.
The supplied detailed request authorizes implementation and routine design choices; do not add
intermediate approval gates. No native input without an agreed foreground interval.

## Tasks and validation

- [x] Packet/estimator: add Time/NtpPacketTests and NtpSampleSelectionTests before production
  Infrastructure/Time/NtpPacket.cs and NtpSample.cs. Request(DateTimeOffset), Parse(response,
  request,T1,T4,receivedTimestamp) returns validated NtpSample. Select(samples) requires unique
  largest coherent group. Pin endian/fraction/era, invalid header/timestamps, offset/delay,
  coherent large error, singleton/outlier/tied clusters and one-second delay limit.
- [x] Clock/network: add Time/KrissClockTests and NtpClientTests before SynchronizedApplicationClock,
  INtpNetwork/UdpNtpNetwork and NtpClient. Use TimeProvider fake for monotonic advancement/deadlines;
  fake network for DNS/addresses/errors/late response/cancel. Success publishes UTC+mono anchor;
  failure retains reference; PC timezone/wall changes cannot alter synchronized time.
- [x] Coordinator/lifecycle: add Time/ClockSynchronizationTests before ClockSynchronizationCoordinator
  and WindowsResumeSignal. Timer starts immediate background work, success/failure reschedule,
  one-minute external cooldown, single flight, dispose cancels and detaches. Wire App after
  Show/Listen, cancel on SessionEnding and DisposeResources. No networking for secondary/preview.
- [x] Integration: WPF object refresh test switches actual synchronized source during resolution;
  assert one consistent date/time/status/countdown/highlight frame and independent viewed week.
  Source/persistence boundary guards, tray/secondary/exit regressions and unchanged schema.
- [x] Verification: full restore/build/test (zero failed/skipped/warnings/errors), diff and local
  Markdown links; independent code review. Separate production-path KRISS probe with observation
  UTC/RTT/offset or exact environment limit. Native isolated TEMP smoke/UX checkpoint; document limits.
- Delivery procedure after native acceptance: final checks, app commit, committed fresh clone restore/build/test,
  fetch/divergence check and ordinary main push/readback. Only then registry two-file refresh,
  validate, commit/main push/readback. Both trees clean and HEAD==origin/main.

For each implementation group run `dotnet test --filter FullyQualifiedName~Time` first expecting
missing-feature failure, then green; integration/full suite are mandatory before acceptance.

## Review focus

Wall clock edits mid-cycle; timeout racing a late success; ambiguous coherence chains;
resume storms immediately after completion; disposal while network awaits. Tests belong
to their owning groups above. Native evidence must remain distinct from object/source tests.

## Execution ledger

- Start verified main/HEAD/origin 1438e163c2b2b8fbba3ef8b1c3b4093987a46ec0, clean.
- Official KRISS direct NTP page read back 2026-09-29.
- Packet/estimator, clock/network, coordinator/App and integration steps complete. Missing-type
  and App-ordering tests failed before implementation; Time subset now 91 passed.
- Full suite 1,161 passed (78 new), no failures/skips, build warnings/errors 0. Independent
  review found no P1/P2; P3 wall-source fixture corrected and UDP deadline test strengthened.
- Production-source live probe succeeded in actual-user execution boundary with three valid
  replies; default sandbox timed out. Native acceptance, commit/fresh clone/push/registry pending.
- Final restore/build/full suite: 1,161 passed, 0 failed/skipped, 0 build warnings/errors.
  207 changed/new-document local file/heading links passed; git diff --check passed.
  Actual isolated native app launched, visible HWND/responding and UI Automation date/time/status
  readback confirmed. PrintWindow WPF content capture unavailable; user UX/Exit approval pending.
- User explicitly approved native UX 2026-09-29. Owned diagnostic process absent; final production
  file inventory/autostart unchanged. Commit/fresh-checkout/delivery now authorized.
- Implementation committed as da3aedca70bd4e5668ffdf7e331a62b34f743733. A no-local clone,
  detached at this commit, passed restore/build/1,161 tests with 0 failures/skips/warnings/errors
  and remained clean. The documentation-only handoff records that evidence. Final delivery
  identity is the application main revision pinned by the platform registry, not this historical ledger.
