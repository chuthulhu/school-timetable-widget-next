# ADR 0017 — Display preset files

Status: **Accepted**. Date: 2026-09-14.
Authority: explicit User Display Preset Import / Export milestone request.
Implementation and verification: [Preset Import / Export](../PRESET-IMPORT-EXPORT.md).

## Decision

Share one user-defined display preset per UTF-8 JSON `.stwpreset` file. The independent
`presetFileVersion` starts at 1 and is not the profile schema version. A file contains the
stable preset ID, normalized user name, layout, four complete typography values and all
format/visibility choices. Font values contain only source, catalog/family ID and family
name. It never contains a profile, timetable, subject/class text, schedules, date overrides,
lunch/view state, clock state, machine/user paths, cache paths, URLs or font bytes.

Export is read-only except for the chosen destination file. In Display Settings it exports
the selected user preset value from the current Draft library—not unrelated unsaved controls.
The button tooltip says this is the saved current editing copy. Built-ins are code-owned and
cannot be exported. Export does not save or publish the profile and does not touch font cache.

Import is select → bounded read → strict parse/validation → preview → explicit decision →
Draft library mutation. The 64 KiB limit is checked before allocation/JSON processing where
the file boundary exposes length. Missing fields, duplicate/unknown properties, future
versions, invalid values and unknown private font IDs reject the whole file. No executable,
external process, path lookup or automatic network/download is involved.

Known bundled and online catalog identities are valid. Missing system or downloaded online
fonts retain canonical identity and preview a fallback/download-needed status; import remains
valid. Unknown source, bundled ID or online ID is invalid. Existing profile/library/display,
network and cache remain unchanged on invalid input.

Import never activates the preset. A unique ID/name adds it to the Draft library. Same ID
requires update, copy or cancel; update retains the stable ID and replaces only that Draft
entry. Copy always generates a new local ID and requires a unique confirmed name. A distinct
ID with a duplicate user name requires a unique name. Built-in labels remain allowed under
ADR 0015 because selectors explicitly label built-in versus user entries; imported values
always remain user entries and cannot overwrite built-ins.

The existing P2 transaction owns the result: Apply/OK persist display and library together;
Cancel/X discards imports and updates after the last successful Apply; save failure retains
the retryable Draft. Profile schema stays v4. Preset file migration/versioning is independent.
