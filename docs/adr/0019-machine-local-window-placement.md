# ADR 0019 — Machine-local window placement and preferred size

Status: **Accepted; implemented, automated verified and user native UX approved**. Date: 2026-09-16.
Authority: explicit Window Placement + Preferred Size Persistence milestone request.

Window geometry is machine-local UI convenience state, separate from portable profile data,
full backups and Display Settings transactions. This supersedes ADR 0003's inclusion of
position Reset in Draft/Cancel and the Product Contract's earlier geometry backup wording.
The separate context-menu reset and completed user window gestures take effect immediately.
Settings Cancel does not undo them. Recovery Required continues blocking profile writes,
while window placement may still be saved independently.

Persist version 1 window-state.json beside profile.json (same isolated directory for DEBUG
TEMP profiles). Store logical preferred width/height, work-area-relative left/top in DIPs,
and a best-effort monitor device hint. Do not store applied geometry, content minimum,
DPI, monitor work-area snapshots, scroll position, viewed week or clock state.

Use current monitor geometry and HWND DPI to derive applied bounds. Resolve the monitor
hint when available, otherwise primary (then first available); clamp on both axes and cap
size to the current taskbar-reserved work area. Negative virtual desktop origins are valid.
Preferred values survive automatic content growth, screen fit, DPI and topology changes.
Shorter content returns to preferred bounds when current constraints permit.

Native enter/moving/sizing/exit messages classify completed normal-state user gestures.
Read the accepted rectangle on completion, recording only changed user-controlled axes.
A move retains preferred size, including after auto growth or DPI transition. A horizontal
resize does not capture auto-grown height. LocationChanged/SizeChanged alone never imply
user intent. Do not fit during the move/size loop or write on every pixel/tick.

Serialize a validated candidate, write/flush/close a unique same-directory temporary file,
then atomically rename using the existing file primitive. Invalid state is ignored without
profile degradation or startup rewrite. Write failure retains runtime intent for retry on
normal close and never reports profile failure or fake persistence success.

No position/size Settings UI, display topology framework, maximize/minimize persistence,
new profile schema, new backup schema or system display changes are introduced.

Verification and native acceptance are recorded separately in [Window Placement](../WINDOW-PLACEMENT.md).
