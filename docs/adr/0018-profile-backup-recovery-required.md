# ADR 0018 — Full profile backup and Recovery Required

Status: **Accepted policy; implementation in progress**. Date: 2026-09-15.
Authority: explicit Full Profile Backup / Restore, Recovery Required and Degraded-Origin
Restore Failure Policy approvals. [Implementation/evidence](../BACKUP-RESTORE.md).

Use independent native `.stwbackup` version 1 containing canonical profile schema v4 inputs.
Export only committed validated values. Restore is full validation, preview, explicit consent,
secured previous state, atomic persistence, whole runtime publication and refresh. Preserve
viewed week and missing font identity; do not download automatically or import legacy backups.

For valid origins, secure one validated pre-restore snapshot and persistent marker before
replacement. If publication fails, attempt whole rollback. If rollback also fails, stop automatic
mutation, preserve marker/source, block mutation and enter Recovery Required. Marker detection
precedes normal startup; syntactically valid candidate bytes do not authorize normal mode.
Use a validated previous snapshot as read-only temporary runtime where possible. Only successful
explicit previous-state recovery clears the marker and restores normal writable mode.

For corrupt/unsupported origins there is no healthy previous profile. Secure exact corrupt
original bytes and hash without normalization or repair. Double failure enters Recovery Required.
Explicit rollback to these bytes returns to the original labeled degraded/write-blocked state,
not healthy recovery. Clear the transaction marker only after exact-byte verification and runtime
restoration. Then allow a new explicit valid backup restore without requiring restart. Retry
failure keeps marker, original and blocking; no automatic retry or default-file creation.

Preserved corrupt originals are retained through successful valid restore in this milestone.
No multi-generation history or failed-candidate history framework. Restore sources are immutable.
Missing/invalid recovery evidence never authorizes destructive fallback.

This extends P9 and ADR 0012 with explicitly approved startup/recovery behavior. The application
clock and existing feature ownership remain separate. Native UX acceptance and implementation
verification are separate from this Accepted policy; see the current P2/test status above.
