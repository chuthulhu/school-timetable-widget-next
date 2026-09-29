# KRISS Application Clock synchronization

Design authority: the 2026-09-29 milestone request and [ADR 0024](adr/0024-kriss-application-clock-sync.md).
Implementation is automated/network verified; user native UX accepted on 2026-09-29.

## Endpoint and scope

Official endpoint: `ntp.kriss.re.kr`, UDP port 123. Read-only official readback on
2026-09-29 confirmed the [KRISS direct NTP guidance](https://www.kriss.re.kr/menu.es?mid=a10305010000).
Only a standard 48-byte NTP request is sent. No UTCk program, source, binary or assets are
used. No timetable, teacher, school, username or profile data is sent; no telemetry is added.
This is best-effort school timetable time, not authenticated/security or precision metrology time.

## Clock and lifecycle design

Keep `IApplicationClock` and the existing immutable `ApplicationTimeSnapshot` contract.
Desktop Infrastructure/Time owns the fallback, synchronized clock, packet codec, network
adapter, sample selection and coordinator. Features know only the common clock.
Use a UTC anchor plus monotonic elapsed time after success, then explicitly represent KST
(UTC+09:00). PC-local fallback retains the PC offset. Windows time/timezone/service/registry
settings are never changed and elevation is never needed.

Single-instance election precedes all synchronization construction. Primary startup populates
the header, shows the window and initializes the tray before starting background synchronization.
Preview clocks keep their synthetic time and do not start NTP. Each UI refresh still reads
one snapshot for date/time/status/countdown/highlight, including a source change mid-refresh.
ViewedWeek and active semester are unaffected.

The immutable runtime reference is replaced atomically. Diagnostic state distinguishes
LocalFallback, Synchronizing and KrissSynchronized, retaining last success and failure category.
Failed resync retains the last good anchor without claiming a new success. Its age is observable;
there is no expiry-to-PC jump. Restart always starts fresh; no correction is persisted.
No new settings, status labels, success toast or network-error popup are introduced.

## Protocol and quality policy

Implement a small .NET DNS/UDP client with no additional package, following
[RFC 5905](https://www.rfc-editor.org/rfc/rfc5905.html). Use T1/T2/T3/T4:
offset = ((T2-T1)+(T3-T4))/2; delay = (T4-T1)-(T3-T2).
Use one cycle-local UTC/monotonic reference so a PC wall-time change during a request
does not distort elapsed time or coherence. Timestamp fractions use integer arithmetic.
Unfold NTP seconds to the era nearest the reference instant (requires a reference within
approximately 68 years); test 2026 and both sides of the 2036 rollover.

Reject short packets, versions other than 3/4, non-server mode, unsynchronized leap,
stratum outside 1–15 (including Kiss-o'-Death), mismatched originate bytes, zero receive/
transmit timestamps, reversed server times, negative or over-1-second network delay,
and unexpected source endpoint/port. Integer fraction decoding cannot produce NaN or overflow.

Collect three samples, spaced one second apart, using fresh DNS each cycle. Try at most
two distinct A/AAAA addresses per sample, rotating the first address by sample. DNS and each
exchange have a two-second deadline; the whole cycle has a 20-second cancellation deadline.
Select a unique largest group whose offset range is at most 250 ms, requiring at least
two members. Ambiguous equally sized groups fail. Use the lowest-delay member of that group.
This rejects single outliers while accepting coherent errors of minutes/hours; absolute offset
is not a rejection rule. These are tunable implementation policy, not precision guarantees.

## Scheduling and shutdown

Initial background request immediately after primary UI setup. Schedule from cycle completion:
success 60 minutes, failure 15 minutes. A single-flight coordinator coalesces simultaneous
requests and suppresses external/resume triggers within one minute of the previous start.
One-shot timers and deadlines use injectable TimeProvider; tests advance fake time, never Sleep.
Windows resume event ownership is isolated in Infrastructure/Windows and unsubscribed on disposal.
Hidden tray state leaves synchronization alive. Show performs the existing immediate refresh.
Exit/SessionEnding cancel without synchronously waiting for DNS/UDP timeouts, dispose the timer
and unsubscribe events. A late response after cancellation cannot update the clock.

## Persistence and future consumers

All state is runtime-only, even in degraded/Recovery Required mode. No profile save, recovery
marker update, window/tray write, autostart registration or semester change is triggered.
Profile v5, .stwbackup v1 and .stwpreset v1 remain unchanged.
Future notifications must use this same Application Clock and retain P5 dedup/resume policy.
Teacher Profile/Group remains FOUNDATION ONLY / IMPLEMENTATION DEFERRED.

## Verification record

2026-09-29: restore/build/test exited 0; 1,161 passed (baseline 1,083 + 78 new), failed/skipped 0,
build warnings/errors 0. Tests cover packet/fraction/era/offset validation, coherent/outlier/large
correction, DNS/UDP failures, address fallback, operation/cycle deadlines, cancellation and late
responses, KST/monotonic/atomic reference, schedule/single-flight/resume/disposal, actual source
changes mid-WPF-refresh, current-day override/highlight/viewed-week consistency, profile v5
bytes/mtime, machine-local sentinels and degraded/Recovery Required no-save behavior.
Existing tray/single-instance/backup/preset/autostart/semester regressions pass unchanged.
New integration tests explicitly keep synchronization running while the fake tray window is
hidden, read the current revision on Show, cancel on Exit, and exclude secondary startup.
These are pure/object/event/source tests, not native input or Windows sleep evidence.

Independent read-only review found no P1/P2. A P3 coverage issue (the wall-change fixture
previously changed an unused wall source) was corrected to use a live mutable fallback.
The UDP test also observes address fallback after the injected two-second deadline.
Self-audit found no system clock/OS time-service mutation, product elevation, UTCk, deprecated
endpoint/IP, per-tick network, singleton trust, profile/schema/settings/UI additions, teacher
implementation or notification scope expansion. NTP is not cryptographically trusted.

Live production-source probe at **2026-09-29T02:12:47.2540899Z**: hostname resolved two addresses;
three replies passed production parser validation. Transport RTTs: 21.7633, 13.1735, 9.8636 ms.
Selected client estimate: offset **+75.9520 ms**, network delay **9.8592 ms**; resulting snapshot KST.
Command: `scripts/probe-kriss-time.ps1`, using the actual production client/codec/transport.
Default sandbox probe at 02:10:56.9268174Z resolved DNS but timed out (zero valid replies);
the actual-user execution boundary succeeded. This standalone probe does not establish
native app synchronization/rendering. No profile or system time-setting API was used.

Native preparation (2026-09-29): directly launched the actual Debug application with a unique
TEMP profile and production window flags. Input-idle was observed after 1,134 ms; this measures
input-idle, not first rendered frame or the exact sync completion time. Read-only Win32 discovery
found a visible 800x600 application window at (40,40), with a responsive process. PrintWindow
returned an image containing the frame but blank WPF content: **capture limitation**, not proof
that the actual widget was blank. Read-only UI Automation independently returned date
`2026년 09월 29일`, time `11:22:53`, status `3교시 · 종료까지 27분` and timetable date headers.
No automated activation, pointer/key injection or clipboard modification was performed.
The normal production directory had zero top-level files at the protection baseline; its
inventory and the exact owned autostart value remained unchanged during this readback.
There were therefore no original profile file bytes to hash-compare in this native run.
Automated TEMP tests separately verify existing profile/window/tray bytes and mtimes.

User explicitly replied “승인” on 2026-09-29 after the requested startup/visual stability,
X-hide, tray-show and normal tray Exit checkpoint. The owned diagnostic process was absent
at final readback; production file inventory and the owned autostart value remained unchanged.
This is limited native UX acceptance, not millisecond timing or every OS/environment coverage.
Failure/later-success behavior has object/event test evidence, not native injected-network
rendering evidence. Actual Windows sleep/resume and timing accuracy across long sleep were not
exercised. Committed fresh-checkout and delivery verification are recorded in
[Continuity verification](CONTINUITY-VERIFICATION.md). No owned native diagnostic app remains running.
