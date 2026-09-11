# ADR 0016 — Bundled and downloaded font resolution

Status: **Accepted**. Date: 2026-09-11.
Authority: explicit Bundled + Online Font Catalog milestone; follow-up authorization to fix the preset-selector P2.
Accepted records requested behavior and storage choices; native UX approval remains a separate checkpoint.

## Decision

FontSelection explicitly separates System, Bundled and OnlineDownloaded, with source plus stable FamilyId
and family name. Time, Date, Weekday and Status are independent. Static curated catalog metadata belongs to
the app, downloaded bytes to regenerable local cache, and selected identities to profile/custom presets.
No runtime path/URL is canonical profile data. System IDs are family names, private IDs are catalog keys.

Bundle unmodified licensed desktop TTF resources for Pretendard, DSEG7 Modern and DSEG7 Classic.
Offer pinned Orbitron and IBM Plex Mono through explicit Settings download, bounded HTTPS/no redirects,
exact size and SHA-256 verification before atomic cache rename. Include the required OFL/copyright notices
in app output. [Font Catalog](../FONT-CATALOG.md) records exact provenance, licensing evidence and checksums.
Do not install fonts system-wide or require admin privileges. WOFF/variable font support is not assumed.

Startup and render are local/offline. Known missing/corrupt cache uses safe rendering fallback and a
Settings re-download message, preserving identity and writable profile state. Unknown private IDs are
invalid model inputs. New online choices enter canonical Draft only after download/resolve succeeds;
while pending, the picker explicitly describes that the prior selection remains active. Apply rejects a
new unavailable online identity, but existing persisted display/preset references remain valid offline.

Settings Preview/Apply/Cancel owns display and the Draft preset library. A completed font download is a
reusable cache side effect and survives Cancel. Disposed Drafts cancel in-flight downloads and cannot be
mutated by late completion. Successful downloads invalidate header resolution independently of identity
changes. Ordinary ticks do no font I/O or network calls. Core and Application Clock remain unchanged.

## Storage and selection

Write schema v4 because v3's strict font DTO/source semantics cannot represent stable private IDs safely.
Strict separate v1/v2/v3 readers retain exact old validation and migrate System identities in memory without
rewriting on load. Subsequent user save writes the complete v4 profile; no downgrade support or partial commit.

Preset library revisions expose stable choice snapshots. The UI resolves the canonical preset reference by
ID into the current choices and presents that item. Collection-reset nulls are not edits; actual non-null
user selections update the same preset reference. Save As selects the new ID without saving, Reset restores
its payload, Cancel rolls back the library, and Apply saves display/library together. This resolves the
observed WPF null-selection P2 without changing ADR 0003/0015 transaction semantics.

## Scope

No arbitrary URLs, font marketplace/search/accounts, local font import, cache-management UI, preset
import/export or themes. Existing future Title/independent AM-PM extension is unchanged.
Implementation/evidence: [Font Catalog](../FONT-CATALOG.md).
