# Project map

For codebase work, consult `graphify-out/GRAPH_TREE.html`, `graphify-out/GRAPH_REPORT.md` and query `graphify-out/graph.json` first. Use the map to locate relevant modules, then read the actual source before editing; the map is not runtime/test evidence.

After changing source or dependency/config code, run the interpreter in `graphify-out/.graphify_python` with `Tools/update_project_map.py`. It compares content hashes and does nothing when code/config is unchanged. Do not rebuild for questions, documentation-only edits, or merely opening a session. Do not start a background watcher or paid semantic API automatically.

If `Docs/Network_Architecture.vi.md` changes, re-extract its semantic fragment on the next code-map update; do not treat stale document nodes as verified implementation. Review parser/graph health warnings in the report. Keep existing browser/delegation instructions from the user's session.

## Camera state after tasks

Always restore the normal Follow Player camera before completing a task. Temporary overview/free/debug cameras may be used during development or testing, but must not remain enabled in the final scene, runtime defaults, or delivered build. Preserve spectator camera behavior while spectating. If a build was made with a temporary camera, rebuild with Follow Player before handing that build over; if rebuilding is outside the authorized scope, explicitly mark that build as outdated rather than presenting it as updated.

## Windows player packaging

Release players include only `Assets/_JungleMaze/Scenes/Boot.unity` and `Assets/_JungleMaze/Scenes/Game.unity`. Keep reference/sample scenes in source, outside the release scene list. When using Unity MCP `manage_build`, pass `options=["compress_lz4", "strict_mode", "detailed_report"]` explicitly; its default `BuildOptions.None` does not preserve the compressed player packaging. Use a fresh output folder and verify runtime data/provenance plus launch/exit before replacing a tested player.
