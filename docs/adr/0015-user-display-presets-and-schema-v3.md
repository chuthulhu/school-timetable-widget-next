# ADR 0015 — User display presets and schema v3

Status: **Accepted**. Date: 2026-09-11.
Authority: explicit User-defined Display Presets milestone request.
This accepts the requested semantics and storage design. User native UX acceptance was
confirmed on 2026-09-11; evidence methods and limits are recorded in Display Settings.

## Decision

A user preset is an immutable Desktop value with a non-empty Guid, normalized display name,
and all current display inputs. DisplayPresetReference represents exactly one built-in enum
or user Guid. Rendering continues to use DisplayConfiguration, never a library lookup.
The four built-in definitions remain code-owned and immutable; they are not saved as library
entries. Names are trimmed, normalized to Unicode NFC, limited to 60 UTF-16 code units and
compared OrdinalIgnoreCase within the user library. Blank and control-character names fail.
Timetable user text is not normalized. Names matching built-in labels are allowed because
selectors explicitly distinguish 기본 제공 and 내 프리셋.

Save As captures valid current Draft and selects the new user preset as its Reset baseline.
Selection copies immutable values into replaceable typography Drafts. Independent edits do
not update the template. Rename keeps the Guid. Explicit update replaces only the selected
user template from valid Draft. Reset previews the selected template's latest saved-in-session
payload. No implicit template/display linkage or extra display commit is introduced.

Delete opens a small picker/confirmation containing only user presets. It does not change
the active display reference. The active preset cannot be deleted: its confirmation button
is disabled with an instruction to first select another style in Display Settings. Other
entries can be selected and explicitly deleted. This resolves the need to target an inactive
preset without adding an alternate rename/update management workflow. Built-ins have no
rename/update operation and never appear in the deletion list.

DisplaySettingsSession owns immutable Draft library revisions and last-Apply library baseline
alongside display Draft. Open starts at committed values. Apply/OK submit both values in one
complete ProfileSnapshot to the existing durable save boundary. Only success publishes both
committed values and advances both baselines. Cancel/X discard all subsequent library edits
and restore display preview. Failed save retains both Draft values and old commitment.
Other feature saves preserve committed display/library; display saves preserve latest other
feature inputs. No new global state, Core responsibility, provider framework or timer.

## Storage

Schema 3 profile adds displayPresets: [{ id, name, settings }]. display is
{ preset: { kind, builtIn, userId }, settings }. References require exactly one matching kind
and identity; the unused identity is null. Settings includes layout, four typographies and
all five format/visibility booleans, without another template reference. Guid uses D format.
Whole-profile validation rejects missing/null/unknown fields, invalid/duplicate IDs, invalid
or normalized duplicate names, invalid payload/font identity and dangling active references.
No partial library recovery or automatic profile rewrite. Missing installed fonts remain
valid logical identities with the existing rendering fallback; no font binary is saved.

Strict separate v1/v2 DTOs retain their original shapes. V1 injects Standard plus empty
library; v2 retains all existing display values and injects empty library. Both remain
writable, without startup rewrite/degradation. The next successful feature save writes v3,
retaining timetable strings, time ticks, date components and lunch exactly. Schema 1/2-only
binaries may reject v3. Existing expected-byte guard, writer lease and atomic save are unchanged.

Preset import/export, bundled fonts, online catalog and downloads remain PLANNED.
Evidence and native limitations: [Display Settings](../DISPLAY-SETTINGS.md).
